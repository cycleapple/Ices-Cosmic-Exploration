using System.IO;

namespace ICE.Compat;

public static class CandidateIdentity
{
    public const string InternalName = "ICECandidate";
    public const string Command = "/icecandidate";
    public const string ConfigDirectory = "ICE.Next.API13";
    public const string StableInternalName = "ICE";

    public static string LegacyConfigPath(string pluginConfigDirectory) => Path.Combine(
        Directory.GetParent(Path.GetFullPath(pluginConfigDirectory))!.FullName,
        StableInternalName, "Mission Config.yaml");
}
