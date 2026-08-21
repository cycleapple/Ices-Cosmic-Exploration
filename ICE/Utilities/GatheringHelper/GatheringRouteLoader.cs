using ICE.Resources.GatheringRoutes;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ICE.Utilities.GatheringHelper;

public static class GatheringRouteLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    // Cache for loaded routes
    private static Dictionary<uint, Dictionary<Vector2, List<GathNodeInfo>>>? _cachedRoutes;

    public static Dictionary<uint, Dictionary<Vector2, List<GathNodeInfo>>> LoadAllRoutes()
    {
        if (_cachedRoutes != null)
            return _cachedRoutes;

        _cachedRoutes = new Dictionary<uint, Dictionary<Vector2, List<GathNodeInfo>>>();
        var assembly = Assembly.GetExecutingAssembly();

        // Get all embedded .yaml files
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(r => r.Contains("GatheringRoutes") && r.EndsWith(".yaml"))
            .ToList();

        PluginLog.Information($"Found {resourceNames.Count} gathering route resources");

        foreach (var resourceName in resourceNames)
        {
            try
            {
                var route = LoadRouteFromResource(resourceName);

                if (!_cachedRoutes.ContainsKey(route.ZoneId))
                    _cachedRoutes[route.ZoneId] = new Dictionary<Vector2, List<GathNodeInfo>>();

                _cachedRoutes[route.ZoneId][route.Flag] = route.Nodes;

                PluginLog.Debug($"Loaded route: Zone {route.ZoneId}, Flag ({route.Flag.X}, {route.Flag.Y}), Job {route.Job}");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to load route from {resourceName}: {ex.Message}");
            }
        }

        PluginLog.Information($"Loaded {_cachedRoutes.Sum(x => x.Value.Count)} gathering routes across {_cachedRoutes.Count} zones");

        return _cachedRoutes;
    }

    /// <summary>
    /// Loads complete embedded route records for one territory, including audit metadata
    /// intentionally omitted from the runtime route cache.
    /// </summary>
    public static List<GatheringRouteFile> LoadRouteFiles(uint territoryId)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var routes = new List<GatheringRouteFile>();

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(r => r.Contains("GatheringRoutes") && r.EndsWith(".yaml")))
        {
            try
            {
                var route = LoadRouteFromResource(resourceName);
                if (route.ZoneId == territoryId)
                    routes.Add(route);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to load route from {resourceName}: {ex.Message}");
            }
        }

        return routes;
    }

    private static GatheringRouteFile LoadRouteFromResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();

        return Deserializer.Deserialize<GatheringRouteFile>(yaml)
            ?? throw new InvalidDataException($"Failed to deserialize {resourceName}");
    }

    // Get routes for a specific zone and flag
    public static List<GathNodeInfo>? GetRoute(uint zoneId, Vector2 flag)
    {
        var routes = LoadAllRoutes();

        if (routes.TryGetValue(zoneId, out var zoneRoutes))
        {
            if (zoneRoutes.TryGetValue(flag, out var nodes))
                return nodes;
        }

        return null;
    }

    // Clear cache if needed (for testing/reloading)
    public static void ClearCache()
    {
        _cachedRoutes = null;
    }

    private static readonly ISerializer Serializer = new SerializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .Build();

    public static void ExportRoute(uint zoneId, Vector2 flag, string? exportPath = null)
    {
        var routes = LoadAllRoutes();

        if (!routes.TryGetValue(zoneId, out var zoneRoutes))
            throw new InvalidOperationException($"Zone {zoneId} not found");

        if (!zoneRoutes.TryGetValue(flag, out var nodes))
            throw new InvalidOperationException($"Route at flag ({flag.X}, {flag.Y}) not found");

        if (nodes == null || nodes.Count == 0)
            throw new InvalidOperationException("Route has no nodes to export");

        var (zoneName, job) = GetRouteMetadata(zoneId, flag);

        // Ensure we have valid values
        if (string.IsNullOrWhiteSpace(zoneName))
            zoneName = "Unknown";
        if (string.IsNullOrWhiteSpace(job))
            job = "BTN";

        var routeFile = new GatheringRouteFile
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            Job = job,
            Flag = flag,
            Author = string.IsNullOrWhiteSpace(C.AuthorName) ? "Ice" : C.AuthorName,
            DateModified = DateTime.UtcNow,
            Nodes = nodes
        };

        // Determine base output path
        string basePath;
        if (!string.IsNullOrEmpty(exportPath))
        {
            basePath = exportPath;
        }
        else if (!string.IsNullOrEmpty(C.CustomRoutePath))
        {
            basePath = C.CustomRoutePath;
        }
        else
        {
            basePath = GetDefaultExportPath();
        }

        // Create zone subdirectory: "ZoneId_ZoneName"
        string zoneFolderName = SanitizeFolderName($"{zoneId}_{zoneName}");
        string outputPath = Path.Combine(basePath, zoneFolderName);

        string fileName = $"{job}_Flag_{(int)flag.X}_{(int)flag.Y}.yaml";
        string fullPath = Path.Combine(outputPath, fileName);

        try
        {
            Directory.CreateDirectory(outputPath);

            string yaml = Serializer.Serialize(routeFile);
            File.WriteAllText(fullPath, yaml);

            PluginLog.Information($"Exported route to {fullPath}");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to write file: {ex.Message}");
            throw new InvalidOperationException($"Failed to write file to {fullPath}: {ex.Message}", ex);
        }
    }
    private static string SanitizeFolderName(string folderName)
    {
        // Remove invalid characters from folder name
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = string.Join("_", folderName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }

    public static void ExportAllRoutes(string? exportPath = null)
    {
        var routes = LoadAllRoutes();
        string outputPath = exportPath ?? C.CustomRoutePath ?? GetDefaultExportPath();

        int exportedCount = 0;

        foreach (var (zoneId, zoneRoutes) in routes)
        {
            foreach (var (flag, nodes) in zoneRoutes)
            {
                try
                {
                    ExportRoute(zoneId, flag, outputPath);
                    exportedCount++;
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Failed to export route for zone {zoneId}, flag ({flag.X}, {flag.Y}): {ex.Message}");
                }
            }
        }

        PluginLog.Information($"Exported {exportedCount} routes to {outputPath}");
    }

    private static string GetDefaultExportPath()
    {
        // Get Dalamud config directory
        var configDir = Svc.PluginInterface.ConfigDirectory.FullName;
        return Path.Combine(configDir, "ExportedRoutes");
    }

    private static (string zoneName, string job) GetRouteMetadata(uint zoneId, Vector2 flag)
    {
        // We need to parse the original resource to get the metadata
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(r => r.Contains("GatheringRoutes") && r.EndsWith(".yaml"))
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            try
            {
                var route = LoadRouteFromResource(resourceName);
                if (route.ZoneId == zoneId && route.Flag == flag)
                {
                    return (route.ZoneName, route.Job);
                }
            }
            catch
            {
                // Skip invalid resources
            }
        }

        return ("Unknown", "BTN"); // Fallback
    }
}
