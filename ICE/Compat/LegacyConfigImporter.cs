using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using ICE.ConfigFiles;

namespace ICE.Compat;

public static class LegacyConfigImporter
{
    public sealed record ImportResult(string Json, string[] Warnings);
    private static JToken? Read(JObject obj, string key) => obj.GetValue(key, StringComparison.OrdinalIgnoreCase);
    private static bool Bool(JObject obj, string key, bool fallback = false) => Read(obj, key)?.Value<bool>() ?? fallback;
    private static int Number(JObject obj, string key, int fallback = 0) => Read(obj, key)?.Value<int>() ?? fallback;

    public static ImportResult ConvertYaml(string yaml)
    {
        var parsed = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<object>(yaml);
        var old = JObject.FromObject(parsed ?? throw new InvalidDataException("舊設定檔是空的。"));
        var converted = new JObject();
        var warnings = new List<string>();
        var newNames = typeof(Config).GetProperties().ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var property in old.Properties())
            if (newNames.TryGetValue(property.Name, out var target)
                && target.Name is not ("MissionConfig" or "ConfigVersion"))
                converted[target.Name] = property.Value.DeepClone();

        converted["OnlyGrabMission_Debug"] = Bool(old, "OnlyGrabMission");
        converted["UseHubReturn"] = false;
        converted["UseAethernet"] = false;
        converted["StartUponEnterMoon"] = false;
        converted["ConfigVersion"] = 1;
        converted["Config_Versioning"] = 4;
        converted["MigratedOldArtisan"] = true;
        converted["SelectedMode"] = Bool(old, "XPRelicGrind") ? 1 : 0;
        var missions = new JObject();
        if (Read(old, "CraftProfiles") is JObject savedProfiles && savedProfiles.Count > 0)
            warnings.Add("製作設定檔已展開至各配方；原始設定檔名稱與內容保存在 YAML 備份中。");
        var profiles = Read(old, "CraftProfiles") as JObject ?? new();
        if (Read(old, "MissionConfig") is JObject oldMissions)
        foreach (var entry in oldMissions.Properties())
        {
            if (!uint.TryParse(entry.Name, out _) || entry.Value is not JObject mission)
                throw new InvalidDataException("任務設定格式不正確。");
            var next = new JObject();
            var missionNames = typeof(Config.MissionSettings).GetProperties().ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var property in mission.Properties())
                if (missionNames.TryGetValue(property.Name, out var target) && target.Name != "CraftSettings")
                    next[target.Name] = property.Value.DeepClone();
            next["GProfileId"] = Number(mission, "GProfileId", Number(mission, "GatherProfileId"));
            next["TurninGoal"] = Bool(mission, "TurninGold") ? 3 : Bool(mission, "TurninSilver") ? 2 : Bool(mission, "TurninBronze") ? 1 : 6;
            if (Bool(mission, "ManualMode") || !Bool(mission, "AutoTurnin", true))
            {
                next["Enabled"] = false;
                warnings.Add($"任務 {entry.Name} 原為手動／不自動繳交，匯入後保持停用，請重新確認。");
            }
            next["TotalAttempts"] = Number(mission, "TotalCompletions") + Number(mission, "FailedCounters");
            var crafts = new JObject();
            if (Read(mission, "CraftSettings") is JObject oldCrafts)
            foreach (var craft in oldCrafts.Properties())
            {
                if (craft.Value is not JObject settings) throw new InvalidDataException("製作設定格式不正確。");
                var profileId = Number(settings, "CraftProfileId", -1);
                if (profileId >= 0 && Read(profiles, profileId.ToString()) is JObject profile
                    && Read(profile, "Settings") is JObject profileSettings)
                    settings = profileSettings;
                else if (profileId >= 0)
                    warnings.Add($"配方 {craft.Name} 的製作設定檔 {profileId} 不存在，保留配方本身的設定。");
                crafts[craft.Name] = new JObject
                {
                    ["UseGlobal"] = false,
                    ["SolverOverrideName"] = Read(settings, "SolverName")?.Value<string>() ?? "",
                    ["FoodId"] = Number(settings, "FoodId"),
                    ["FoodHQ"] = Bool(settings, "FoodHq", true),
                    ["PotionId"] = Number(settings, "PotionId"),
                    ["PotionHQ"] = Bool(settings, "PotionHq", true)
                };
            }
            next["CraftSettings"] = crafts;
            missions[entry.Name] = next;
        }
        converted["MissionConfig"] = missions;
        var agenda = new JArray();
        if (Read(old, "CosmicAgenda") is JArray oldAgenda)
        foreach (var token in oldAgenda)
        {
            if (token is not JObject entry) throw new InvalidDataException("宇宙計畫格式不正確。");
            var goalToken = Read(entry, "Goal");
            var goal = goalToken?.ToString() switch
            {
                "RelicStage" or "0" => 0, "CosmoCredits" or "1" => 1,
                "ClassLevel" or "2" => 2, "ClassScore" or "3" => 3,
                "StandardMissionsGolded" or "4" => 4, "LunarCredits" or "5" => 5, _ => -1
            };
            var job = Number(entry, "Job", 8);
            var target = Number(entry, "Target", 1);
            if (goal < 0 || job < 8 || job > 18 || target < 1)
                throw new InvalidDataException("宇宙計畫含無法辨識的目標、職業或目標值。");
            if (goal == 4 && new[]{"A", "B", "C", "D"}.Any(rank =>
                !(Read(entry, "StandardGold" + rank + "Rank")?.Value<bool?>() ?? Bool(old, "StopStandardGold" + rank + "Rank", true))))
                throw new InvalidDataException("此宇宙計畫只要求部分任務等級金牌，新版尚無對應條件；請保留舊版設定並重新建立計畫。");
            agenda.Add(new JObject
            {
                ["SelectedOption"] = new[]{5, 6, 9, 10, 11, 7}[goal],
                ["SelectedMode"] = goal == 0 ? 1 : goal == 2 ? 2 : goal == 4 ? 4 : 0,
                ["SelectedJob"] = job,
                ["SelectedRelicLevel"] = target, ["CreditAmount"] = target,
                ["PlanetAmount"] = target, ["ClassLevel"] = target, ["ClassScore"] = target
            });
        }
        converted["Cosmic_Agenda"] = agenda;
        if (Bool(old, "CosmicAgendaMode") && agenda.Count > 0) converted["SelectedMode"] = 10;
        if (Bool(old, "StopOnceStandardMissionsGolded") && new[]{"A", "B", "C", "D"}.Any(rank => !Bool(old, "StopStandardGold" + rank + "Rank", true)))
            throw new InvalidDataException("部分任務等級金牌停止條件尚無對應設定，為避免改變執行範圍，未自動匯入。");
        converted["LegacyImportWarnings"] = new JArray(warnings);
        // Deserialize the real target schema now, before writing any file.
        var config = converted.ToObject<Config>() ?? throw new InvalidDataException("設定轉換失敗。");
        return new(JsonConvert.SerializeObject(config, Formatting.Indented), warnings.ToArray());
    }

    public static string SourceHash(string source) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant();

    public static ImportResult ReplaceImport(string source, string destination, string expectedHash)
    {
        var bytes = File.ReadAllBytes(source);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(hash, expectedHash, StringComparison.Ordinal))
            throw new InvalidDataException("舊設定已在預覽後變更，請重新預覽再匯入。");
        var result = ConvertYaml(System.Text.Encoding.UTF8.GetString(bytes));
        WriteImport(source, destination, bytes, result, replace: true);
        return result;
    }

    public static bool TryImport(string source, string destination)
    {
        if (!File.Exists(source) || File.Exists(destination)) return false;
        var bytes = File.ReadAllBytes(source);
        var result = ConvertYaml(System.Text.Encoding.UTF8.GetString(bytes));
        WriteImport(source, destination, bytes, result, replace: false);
        return true;
    }

    private static void WriteImport(string source, string destination, byte[] bytes, ImportResult result, bool replace)
    {
        var folder = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(folder);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var backup = Path.Combine(folder, $"Legacy-Mission-Config-{hash[..12]}.yaml");
        if (!File.Exists(backup)) File.WriteAllBytes(backup, bytes);
        if (replace && File.Exists(destination))
            File.Copy(destination, destination + ".before-import-" + Guid.NewGuid().ToString("N") + ".json");
        var report = JsonConvert.SerializeObject(new { Source = source, SourceSha256 = hash, result.Warnings }, Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "Legacy-Import-Report.json"), report);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, result.Json);
            File.Move(temporary, destination, overwrite: replace);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
