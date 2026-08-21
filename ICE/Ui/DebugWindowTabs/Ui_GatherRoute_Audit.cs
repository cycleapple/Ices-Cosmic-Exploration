using ECommons.GameHelpers;
using ICE.Utilities.GatheringHelper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ICE.Ui.DebugWindowTabs;

internal static class Ui_GatherRoute_Audit
{
    private static readonly object StateLock = new();
    private static CancellationTokenSource? cancellation;
    private static Task? auditTask;
    private static GatheringRouteAuditProgress progress;
    private static GatheringRouteAuditReport? report;
    private static string? error;
    private static bool showPassedNodes;
    private static string nodeFilterText = string.Empty;

    public static void Draw()
    {
        ImGui.Text("Sinus Ardorum Gathering Route Audit");
        ImGui.TextWrapped("Reads the embedded territory 1237 routes and checks sampled approach points against the currently loaded vnavmesh. This does not start a mission, move the player, or edit route data.");
        ImGui.Separator();

        var isRunning = auditTask is { IsCompleted: false };
        ImGui.Text($"Current territory: {Player.Territory} (required: {GatheringRouteAuditor.SupportedTerritory})");
        ImGui.Text($"vnavmesh: {(P.Navmesh.Installed ? (P.Navmesh.IsReady() ? "Ready" : "Not ready") : "Not installed")}");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##MoonRouteAuditNodeFilter", ref nodeFilterText, 512);
        ImGui.TextWrapped("Optional Node IDs (comma/space separated). Leave empty to scan all Moon nodes.");

        if (!isRunning)
        {
            if (ImGui.Button("Start Moon Route Audit"))
                StartAudit();
        }
        else
        {
            if (ImGui.Button("Cancel Audit"))
                cancellation?.Cancel();
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Results"))
        {
            lock (StateLock)
            {
                if (!isRunning)
                {
                    report = null;
                    error = null;
                    progress = default;
                }
            }
        }

        GatheringRouteAuditProgress progressSnapshot;
        GatheringRouteAuditReport? reportSnapshot;
        string? errorSnapshot;
        lock (StateLock)
        {
            progressSnapshot = progress;
            reportSnapshot = report;
            errorSnapshot = error;
        }

        if (isRunning)
        {
            var fraction = progressSnapshot.TotalNodes == 0
                ? 0f
                : (float)progressSnapshot.CompletedNodes / progressSnapshot.TotalNodes;
            ImGui.ProgressBar(fraction, new Vector2(-1, 0),
                $"{progressSnapshot.CompletedNodes}/{progressSnapshot.TotalNodes}");
            ImGui.Text($"Current: {progressSnapshot.CurrentRoute}, node {progressSnapshot.CurrentNodeId}");
        }

        if (!string.IsNullOrWhiteSpace(errorSnapshot))
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.25f, 1f), errorSnapshot);

        if (reportSnapshot == null)
            return;

        var pass = reportSnapshot.Results.Count(result => result.Severity == GatheringRouteAuditSeverity.Pass);
        var review = reportSnapshot.Results.Count(result => result.Severity == GatheringRouteAuditSeverity.Review);
        var warning = reportSnapshot.Results.Count(result => result.Severity == GatheringRouteAuditSeverity.Warning);
        var fail = reportSnapshot.Results.Count(result => result.Severity == GatheringRouteAuditSeverity.Fail);

        ImGui.Separator();
        ImGui.Text($"Completed in {(reportSnapshot.CompletedAt - reportSnapshot.StartedAt).TotalSeconds:0.0}s");
        ImGui.TextColored(new Vector4(0.35f, 1f, 0.35f, 1f), $"PASS {pass}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.45f, 0.75f, 1f, 1f), $"REVIEW {review}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.75f, 0.25f, 1f), $"WARN {warning}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.30f, 0.25f, 1f), $"FAIL {fail}");

        ImGui.TextWrapped($"CSV: {reportSnapshot.OutputPath}");
        if (ImGui.Button("Copy Report Path"))
            ImGui.SetClipboardText(reportSnapshot.OutputPath);
        ImGui.SameLine();
        ImGui.Checkbox("Show PASS rows", ref showPassedNodes);

        DrawResults(reportSnapshot);
    }

    private static void StartAudit()
    {
        HashSet<uint>? nodeFilter = null;
        if (!string.IsNullOrWhiteSpace(nodeFilterText))
        {
            nodeFilter = new HashSet<uint>();
            foreach (var token in nodeFilterText.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!uint.TryParse(token, out var nodeId))
                {
                    lock (StateLock)
                        error = $"Invalid Node ID: {token}";
                    return;
                }
                nodeFilter.Add(nodeId);
            }
        }

        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        lock (StateLock)
        {
            report = null;
            error = null;
            progress = default;
        }

        auditTask = RunAuditAsync(cancellation.Token, nodeFilter);
    }

    private static async Task RunAuditAsync(CancellationToken cancellationToken, HashSet<uint>? nodeFilter)
    {
        try
        {
            var completedReport = await GatheringRouteAuditor.RunAsync(
                update =>
                {
                    lock (StateLock)
                        progress = update;
                },
                cancellationToken,
                nodeFilter);

            lock (StateLock)
                report = completedReport;
        }
        catch (OperationCanceledException)
        {
            lock (StateLock)
                error = "Audit cancelled; no partial report was written.";
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Moon gathering route audit failed: {ex}");
            lock (StateLock)
                error = ex.Message;
        }
    }

    private static void DrawResults(GatheringRouteAuditReport reportSnapshot)
    {
        var rows = reportSnapshot.Results
            .Where(result => showPassedNodes || result.Severity != GatheringRouteAuditSeverity.Pass)
            .OrderByDescending(result => result.Severity)
            .ThenBy(result => result.Job)
            .ThenBy(result => result.NodeId)
            .ToList();

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit;
        if (!ImGui.BeginTable("MoonRouteAuditResults", 9, flags, new Vector2(0, 0)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Status");
        ImGui.TableSetupColumn("Job");
        ImGui.TableSetupColumn("Flag");
        ImGui.TableSetupColumn("Node");
        ImGui.TableSetupColumn("Angles");
        ImGui.TableSetupColumn("Reachable");
        ImGui.TableSetupColumn("Coverage");
        ImGui.TableSetupColumn("Max score");
        ImGui.TableSetupColumn("Notes", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var result in rows)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result.Severity.ToString().ToUpperInvariant());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result.Job);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{result.Flag.X:0}, {result.Flag.Y:0}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result.NodeId.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{result.RadiusStart:0.#}->{result.RadiusEnd:0.#}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{result.ReachableCount}/{result.CandidateCount}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{result.Coverage:P0}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{result.MaxPathScore:0.00}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(result.Notes);
        }

        ImGui.EndTable();
    }
}
