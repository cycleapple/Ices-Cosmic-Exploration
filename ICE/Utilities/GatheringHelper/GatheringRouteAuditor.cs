using ECommons.GameHelpers;
using ICE.Resources.GatheringRoutes;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ICE.Utilities.GatheringHelper;

internal enum GatheringRouteAuditSeverity
{
    Pass,
    Review,
    Warning,
    Fail,
}

internal sealed class GatheringRouteAuditNodeResult
{
    public required string Job { get; init; }
    public required Vector2 Flag { get; init; }
    public required uint NodeId { get; init; }
    public required float RadiusStart { get; init; }
    public required float RadiusEnd { get; init; }
    public required int CandidateCount { get; init; }
    public required int ProjectedCount { get; init; }
    public required int ReachableCount { get; init; }
    public required float MaxHorizontalProjection { get; init; }
    public required float MaxVerticalProjection { get; init; }
    public required float MaxPathScore { get; init; }
    public required GatheringRouteAuditSeverity Severity { get; init; }
    public required string Notes { get; init; }

    public float Coverage => CandidateCount == 0 ? 0f : (float)ReachableCount / CandidateCount;
}

internal sealed class GatheringRouteAuditReport
{
    public required DateTime StartedAt { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required IReadOnlyList<GatheringRouteAuditNodeResult> Results { get; init; }
    public required string OutputPath { get; init; }
}

internal readonly record struct GatheringRouteAuditProgress(
    int CompletedNodes,
    int TotalNodes,
    string CurrentRoute,
    uint CurrentNodeId);

/// <summary>
/// Performs a read-only audit of embedded Sinus Ardorum gathering routes against
/// the currently loaded vnavmesh. It never moves the player or modifies route data.
/// </summary>
internal static class GatheringRouteAuditor
{
    internal const uint SupportedTerritory = 1237;

    private const float AngleStep = 5f;
    private const float DistanceStep = 0.25f;
    // Mirrors Task_Gather: GatheringRange (3.5m) - GatherApproachTolerance (0.5m).
    private const float RuntimeMaxApproachDistance = 3f;
    private const float CandidateProjectionRadius = 0.75f;
    private const float CandidateProjectionHeight = 3f;
    private const float MaxHorizontalProjection = 0.60f;
    private const float MaxVerticalProjection = 3.00f;
    private const float LandZoneProjectionRadius = 2f;
    private const float LandZoneProjectionHeight = 3f;
    private const float MaxLandZoneHorizontalProjection = 1.50f;
    private const float MaxLandZoneVerticalProjection = 2.50f;
    private const int FullPathConfirmationsPerNode = 2;

    public static async Task<GatheringRouteAuditReport> RunAsync(
        Action<GatheringRouteAuditProgress> onProgress,
        CancellationToken cancellationToken,
        HashSet<uint>? nodeFilter = null)
    {
        EnsureCanAudit();

        var startedAt = DateTime.Now;
        var routes = GatheringRouteLoader.LoadRouteFiles(SupportedTerritory)
            .OrderBy(route => route.Job)
            .ThenBy(route => route.Flag.X)
            .ThenBy(route => route.Flag.Y)
            .ToList();
        var totalNodes = routes.Sum(route => route.Nodes.Count(node => nodeFilter == null || nodeFilter.Contains(node.NodeId)));
        if (totalNodes == 0)
            throw new InvalidOperationException("No Moon route nodes matched the Node ID filter.");
        var completedNodes = 0;
        var results = new List<GatheringRouteAuditNodeResult>(totalNodes);

        foreach (var route in routes)
        {
            foreach (var node in route.Nodes)
            {
                if (nodeFilter != null && !nodeFilter.Contains(node.NodeId))
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                EnsureCanAudit();
                onProgress(new(completedNodes, totalNodes, $"{route.Job} ({route.Flag.X:0}, {route.Flag.Y:0})", node.NodeId));

                results.Add(await AuditNodeAsync(route, node, cancellationToken).ConfigureAwait(false));
                completedNodes++;
                onProgress(new(completedNodes, totalNodes, $"{route.Job} ({route.Flag.X:0}, {route.Flag.Y:0})", node.NodeId));

                // Keep the audit cooperative even when the IPC completes synchronously.
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        var completedAt = DateTime.Now;
        var outputPath = WriteReport(startedAt, completedAt, results, nodeFilter);
        return new GatheringRouteAuditReport
        {
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Results = results,
            OutputPath = outputPath,
        };
    }

    private static async Task<GatheringRouteAuditNodeResult> AuditNodeAsync(
        GatheringRouteFile route,
        GathNodeInfo node,
        CancellationToken cancellationToken)
    {
        var notes = new List<string>();
        var angles = SampleAngles(node.RadiusStart, node.RadiusEnd);
        var distances = SampleDistances(node.MinDistance, node.MaxDistance);
        var candidateCount = angles.Count * distances.Count;

        var landZoneProjection = Project(
            node.LandZone,
            LandZoneProjectionRadius,
            LandZoneProjectionHeight,
            MaxLandZoneHorizontalProjection,
            MaxLandZoneVerticalProjection);

        if (landZoneProjection.Point == null)
        {
            notes.Add("LandZone could not be projected within tolerance");
            return CreateResult(route, node, candidateCount, 0, 0, 0f, 0f, 0f,
                GatheringRouteAuditSeverity.Fail, notes);
        }

        var projectedCount = 0;
        var reachableCount = 0;
        var fullPathConfirmations = 0;
        var maxHorizontalProjection = 0f;
        var maxVerticalProjection = 0f;
        var maxPathScore = 0f;
        var projectionIssueAngles = new List<float>();
        var pathFailureAngles = new List<float>();
        var projectionFailureSamples = new List<string>();
        var pathFailureSamples = new List<string>();

        foreach (var angle in angles)
        {
            var projectedAtAngle = new List<(float Distance, Vector3 Point)>();
            foreach (var distance in distances)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requested = PositionAt(node.Position, angle, distance);
                var projection = Project(
                    requested,
                    CandidateProjectionRadius,
                    CandidateProjectionHeight,
                    MaxHorizontalProjection,
                    MaxVerticalProjection);

                maxHorizontalProjection = MathF.Max(maxHorizontalProjection, projection.HorizontalDisplacement);
                maxVerticalProjection = MathF.Max(maxVerticalProjection, projection.VerticalDisplacement);
                if (projection.Point == null)
                {
                    projectionFailureSamples.Add(FormatSample(angle, distance));
                    continue;
                }

                projectedCount++;
                projectedAtAngle.Add((distance, projection.Point.Value));
            }

            if (projectedAtAngle.Count != distances.Count)
                projectionIssueAngles.Add(angle);

            if (projectedAtAngle.Count == 0)
                continue;

            // Score the middle radial sample first. Normally it represents the entire
            // short local ray; projection/path anomalies trigger per-distance scoring.
            var middleDistance = (distances[0] + distances[^1]) / 2f;
            var representative = projectedAtAngle.MinBy(candidate => MathF.Abs(candidate.Distance - middleDistance));
            var representativePath = await ScorePathAsync(
                landZoneProjection.Point.Value,
                representative.Point,
                fullPathConfirmations < FullPathConfirmationsPerNode,
                cancellationToken).ConfigureAwait(false);
            if (representativePath.UsedFullPath)
                fullPathConfirmations++;

            var reachableAtAngle = representativePath.Reachable ? 1 : 0;
            maxPathScore = MathF.Max(maxPathScore, representativePath.Score);
            if (!representativePath.Reachable)
                pathFailureSamples.Add(FormatSample(angle, representative.Distance));
            var requiresRadialChecks = projectedAtAngle.Count != distances.Count || !representativePath.Reachable;
            if (requiresRadialChecks)
            {
                foreach (var candidate in projectedAtAngle)
                {
                    if (MathF.Abs(candidate.Distance - representative.Distance) < 0.001f)
                        continue;

                    var radialPath = await ScorePathAsync(
                        landZoneProjection.Point.Value,
                        candidate.Point,
                        fullPathConfirmations < FullPathConfirmationsPerNode,
                        cancellationToken).ConfigureAwait(false);
                    if (radialPath.UsedFullPath)
                        fullPathConfirmations++;
                    if (radialPath.Reachable)
                        reachableAtAngle++;
                    else
                        pathFailureSamples.Add(FormatSample(angle, candidate.Distance));
                    maxPathScore = MathF.Max(maxPathScore, radialPath.Score);
                }
            }
            else
            {
                reachableAtAngle = projectedAtAngle.Count;
            }

            if (reachableAtAngle != projectedAtAngle.Count)
                pathFailureAngles.Add(angle);
            reachableCount += reachableAtAngle;
        }

        var coverage = candidateCount == 0 ? 0f : (float)reachableCount / candidateCount;
        GatheringRouteAuditSeverity severity;
        if (reachableCount == 0 || coverage < 0.50f)
            severity = GatheringRouteAuditSeverity.Fail;
        else if (coverage < 0.85f)
            severity = GatheringRouteAuditSeverity.Warning;
        else if (reachableCount != candidateCount)
            severity = GatheringRouteAuditSeverity.Review;
        else
            severity = GatheringRouteAuditSeverity.Pass;

        if (projectedCount != candidateCount)
            notes.Add($"projection {projectedCount}/{candidateCount}");
        if (reachableCount != projectedCount)
            notes.Add($"path {reachableCount}/{projectedCount}");
        if (projectionIssueAngles.Count > 0)
            notes.Add($"projection angles {FormatAngles(projectionIssueAngles)}");
        if (projectionFailureSamples.Count > 0)
            notes.Add($"projection samples {string.Join("|", projectionFailureSamples)}");
        if (pathFailureAngles.Count > 0)
            notes.Add($"path angles {FormatAngles(pathFailureAngles)}");
        if (pathFailureSamples.Count > 0)
            notes.Add($"path samples {string.Join("|", pathFailureSamples)}");
        if (notes.Count == 0)
            notes.Add("all sampled candidates reachable");

        return CreateResult(route, node, candidateCount, projectedCount, reachableCount,
            maxHorizontalProjection, maxVerticalProjection, maxPathScore, severity, notes);
    }

    private static GatheringRouteAuditNodeResult CreateResult(
        GatheringRouteFile route,
        GathNodeInfo node,
        int candidateCount,
        int projectedCount,
        int reachableCount,
        float maxHorizontalProjection,
        float maxVerticalProjection,
        float maxPathScore,
        GatheringRouteAuditSeverity severity,
        List<string> notes)
        => new()
        {
            Job = route.Job,
            Flag = route.Flag,
            NodeId = node.NodeId,
            RadiusStart = node.RadiusStart,
            RadiusEnd = node.RadiusEnd,
            CandidateCount = candidateCount,
            ProjectedCount = projectedCount,
            ReachableCount = reachableCount,
            MaxHorizontalProjection = maxHorizontalProjection,
            MaxVerticalProjection = maxVerticalProjection,
            MaxPathScore = maxPathScore,
            Severity = severity,
            Notes = string.Join("; ", notes),
        };

    private static (Vector3? Point, float HorizontalDisplacement, float VerticalDisplacement) Project(
        Vector3 requested,
        float searchRadius,
        float searchHeight,
        float maxHorizontalDisplacement,
        float maxVerticalDisplacement)
    {
        Vector3? projected = null;
        try
        {
            projected = P.Navmesh.NearestPoint(requested, searchRadius, searchHeight);
            projected ??= P.Navmesh.PointOnFloor(requested, false, searchHeight);
        }
        catch
        {
            return (null, float.PositiveInfinity, float.PositiveInfinity);
        }

        if (projected == null)
            return (null, float.PositiveInfinity, float.PositiveInfinity);

        var delta = projected.Value - requested;
        var horizontal = MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
        var vertical = MathF.Abs(delta.Y);
        return horizontal <= maxHorizontalDisplacement && vertical <= maxVerticalDisplacement
            ? (projected, horizontal, vertical)
            : (null, horizontal, vertical);
    }

    private static async Task<(bool Reachable, bool UsedFullPath, float Score)> ScorePathAsync(
        Vector3 from,
        Vector3 to,
        bool allowFullPathConfirmation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directDistance = Vector3.Distance(from, to);
        if (directDistance < 0.10f)
            return (true, false, 0f);

        try
        {
            var score = await P.Navmesh.PathfindScore(from, to, false).ConfigureAwait(false);
            if (float.IsFinite(score) && score >= 0f)
                return (true, false, score);
        }
        catch
        {
            // A bounded full-path confirmation below distinguishes an unavailable
            // score from a genuinely unreachable sample without flooding vnav.
        }

        if (!allowFullPathConfirmation)
            return (false, false, 0f);

        try
        {
            var path = await P.Navmesh.Pathfind(from, to, false).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (path == null || path.Count < 2)
                return (false, true, 0f);

            var pathDistance = path.Zip(path.Skip(1), Vector3.Distance).Sum();
            return (true, true, pathDistance);
        }
        catch
        {
            return (false, true, 0f);
        }
    }

    private static List<float> SampleAngles(float radiusStart, float radiusEnd)
    {
        var start = NormalizeAngle(radiusStart);
        var end = NormalizeAngle(radiusEnd);
        var span = NormalizeAngle(end - start);
        if (MathF.Abs(start - end) < 0.01f)
            span = 360f;

        var result = new List<float>();
        if (span >= 359.99f)
        {
            for (var offset = 0f; offset < 360f; offset += AngleStep)
                result.Add(NormalizeAngle(start + offset));
            return result;
        }

        for (var offset = 0f; offset < span; offset += AngleStep)
            result.Add(NormalizeAngle(start + offset));
        result.Add(end);
        return result.Distinct().ToList();
    }

    private static List<float> SampleDistances(float minDistance, float maxDistance)
    {
        var configuredMin = MathF.Min(minDistance, maxDistance);
        var configuredMax = MathF.Max(minDistance, maxDistance);
        var max = MathF.Min(configuredMax, RuntimeMaxApproachDistance);
        var min = MathF.Min(configuredMin, max);
        if (MathF.Abs(max - min) < 0.01f)
            return [min];

        var result = new List<float>();
        for (var distance = min; distance < max; distance += DistanceStep)
            result.Add(distance);
        result.Add(max);
        return result.Distinct().ToList();
    }

    private static Vector3 PositionAt(Vector3 center, float angle, float distance)
    {
        var radians = (180f - angle) * (MathF.PI / 180f);
        return new Vector3(
            center.X + distance * MathF.Sin(radians),
            center.Y,
            center.Z + distance * MathF.Cos(radians));
    }

    private static float NormalizeAngle(float angle) => (angle % 360f + 360f) % 360f;

    private static string FormatAngles(IEnumerable<float> angles)
        => string.Join("|", angles.Select(angle => angle.ToString("0.#", CultureInfo.InvariantCulture)));

    private static string FormatSample(float angle, float distance)
        => $"{angle.ToString("0.#", CultureInfo.InvariantCulture)}:{distance.ToString("0.##", CultureInfo.InvariantCulture)}";

    private static void EnsureCanAudit()
    {
        if (Player.Territory != SupportedTerritory)
            throw new InvalidOperationException("The route audit can only run while the player is in Sinus Ardorum (territory 1237).");
        if (!P.Navmesh.Installed)
            throw new InvalidOperationException("vnavmesh is not installed.");
        if (!P.Navmesh.IsReady())
            throw new InvalidOperationException("vnavmesh is not ready for the current territory.");
        if (SchedulerMain.State != IceState.Idle || P.TaskManager.NumQueuedTasks > 0)
            throw new InvalidOperationException("Stop ICE and clear its task queue before running the route audit.");
        if (P.Navmesh.IsRunning() || P.Navmesh.PathfindInProgress())
            throw new InvalidOperationException("Stop active vnavmesh movement before running the route audit.");
    }

    private static string WriteReport(
        DateTime startedAt,
        DateTime completedAt,
        IReadOnlyList<GatheringRouteAuditNodeResult> results,
        HashSet<uint>? nodeFilter)
    {
        var directory = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "GatherRouteAudits");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"SinusArdorum_{completedAt:yyyyMMdd_HHmmss}.csv");
        var builder = new StringBuilder();
        builder.AppendLine($"# territory,1237,Sinus Ardorum");
        builder.AppendLine($"# started,{startedAt:O}");
        builder.AppendLine($"# completed,{completedAt:O}");
        builder.AppendLine($"# angle_step,{AngleStep}");
        builder.AppendLine($"# distance_step,{DistanceStep}");
        builder.AppendLine($"# runtime_max_approach_distance,{RuntimeMaxApproachDistance}");
        builder.AppendLine("# path_scoring,one representative distance per angle; all projected distances on anomalous angles");
        builder.AppendLine($"# node_filter,{(nodeFilter == null ? "all" : string.Join("|", nodeFilter.OrderBy(nodeId => nodeId)))}");
        builder.AppendLine("severity,job,flag_x,flag_y,node_id,radius_start,radius_end,candidates,projected,reachable,coverage,max_horizontal_projection,max_vertical_projection,max_path_score,notes");

        foreach (var result in results.OrderByDescending(result => result.Severity).ThenBy(result => result.Job).ThenBy(result => result.NodeId))
        {
            builder.AppendLine(string.Join(",",
                result.Severity.ToString().ToUpperInvariant(),
                Csv(result.Job),
                result.Flag.X.ToString("0.###", CultureInfo.InvariantCulture),
                result.Flag.Y.ToString("0.###", CultureInfo.InvariantCulture),
                result.NodeId,
                result.RadiusStart.ToString("0.###", CultureInfo.InvariantCulture),
                result.RadiusEnd.ToString("0.###", CultureInfo.InvariantCulture),
                result.CandidateCount,
                result.ProjectedCount,
                result.ReachableCount,
                result.Coverage.ToString("0.000", CultureInfo.InvariantCulture),
                result.MaxHorizontalProjection.ToString("0.000", CultureInfo.InvariantCulture),
                result.MaxVerticalProjection.ToString("0.000", CultureInfo.InvariantCulture),
                result.MaxPathScore.ToString("0.000", CultureInfo.InvariantCulture),
                Csv(result.Notes)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        return path;
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
