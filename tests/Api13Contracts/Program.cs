using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
var root = Path.GetFullPath(args[0]);
var plugin = Path.Combine(root, "ICE/bin/Release");
var artisanDir = Path.GetFullPath(args[1]);
var sdk = Path.GetFullPath(args[2]);
AssemblyLoadContext.Default.Resolving += (ctx, name) => {
    foreach (var dir in new[]{plugin, artisanDir, sdk}) {
        var path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path)) return ctx.LoadFromAssemblyPath(path);
    }
    return null;
};
var ice = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(plugin, "ICE.dll"));
var artisan = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(artisanDir, "Artisan.dll"));
int checks = 0;
void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
Check(ice.GetReferencedAssemblies().Single(x => x.Name == "Dalamud").Version!.Major == 13, "Not API13");
var consumer = ice.GetType("ICE.IPC.ArtisanIPC", true)!;
var provider = artisan.GetType("Artisan.IPC.IPC", true)!;
foreach (var name in new[]{"SetTemporarySolver", "SetTemporaryFood", "SetTemporaryPotion", "ClearTemporaryRecipeSettings", "ClearAllTemporarySettings", "GetAvailableSolvers", "GetRaphaelStatus", "GetRaphaelFailure"}) {
    var invoke = consumer.GetField(name)!.FieldType.GetMethod("Invoke")!;
    var method = provider.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
    Check(invoke.ReturnType == method.ReturnType, name + " return mismatch");
    Check(invoke.GetParameters().Select(x=>x.ParameterType).SequenceEqual(method.GetParameters().Select(x=>x.ParameterType)), name + " argument mismatch");
}
var ah = ice.GetType("ICE.IPC.AutoHookIPC", true)!;
Check(ah.GetField("SwapBaitById")!.FieldType == typeof(Func<uint,bool>), "Bait IPC mismatch");
var instance = RuntimeHelpers.GetUninitializedObject(consumer);
int clears = 0, solverCalls = 0, foodCalls = 0;
bool foodValid = true;
void Set(string name, object value) => consumer.GetField(name)!.SetValue(instance, value);
Set("ClearTemporaryRecipeSettings", (Action<uint>)(_ => clears++));
Set("GetAvailableSolvers", (Func<uint,string[]>)(_ => ["Raphael Recipe Solver"]));
Set("SetTemporarySolver", (Func<uint,string,bool>)((_,_) => { solverCalls++; return true; }));
Set("SetTemporaryFood", (Func<uint,uint,bool,bool>)((_,_,_) => { foodCalls++; return foodValid; }));
Set("SetTemporaryPotion", (Func<uint,uint,bool,bool>)((_,_,_) => true));
var configType = ice.GetType("ICE.ConfigFiles.Config+MissionSettings+ArtisanSettings", true)!;
var config = Activator.CreateInstance(configType)!;
var apply = consumer.GetMethod("AssignArtisanRecipe", BindingFlags.NonPublic | BindingFlags.Instance)!;
bool Apply() => (bool)apply.Invoke(instance, new object[]{(uint)123, config, false})!;
var solverProp = configType.GetProperty("ArtisanSolverType")!;
solverProp.SetValue(config, Enum.Parse(solverProp.PropertyType, "Raphael"));
Check(Apply() && solverCalls == 1 && foodCalls == 1 && clears == 1, "Valid overrides not applied");
foodValid = false;
Check(!Apply() && clears == 3, "Invalid food did not roll back temporary overrides");
foodValid = true;
solverProp.SetValue(config, Enum.Parse(solverProp.PropertyType, "Standard"));
Check(!Apply() && clears == 5 && solverCalls == 2, "Unavailable solver was applied");
configType.GetProperty("ManualId")!.SetValue(config, (uint)42);
Check(!Apply() && clears == 5, "Unsupported manual override was silently ignored");
configType.GetProperty("ManualId")!.SetValue(config, (uint)0);
configType.GetProperty("SolverOverrideName")!.SetValue(config, "Raphael Recipe Solver");
Check(Apply(), "Imported exact solver override was not applied");
configType.GetProperty("SolverOverrideName")!.SetValue(config, "Missing Solver");
Check(!Apply(), "Invalid imported solver was silently accepted");
var importer = ice.GetType("ICE.Compat.LegacyConfigImporter", true)!;
var convert = importer.GetMethod("ConvertYaml")!;
string ConvertYaml(string yaml) => (string)convert.ReturnType.GetProperty("Json")!.GetValue(convert.Invoke(null, new[]{yaml})!)!;
var yaml = """
configVersion: 13
cosmicAgendaMode: true
onlyGrabMission: true
startUponEnterMoon: true
gatherProfiles:
  2: { id: 2, name: Mining }
craftProfiles:
  7:
    name: Raphael
    settings: { solverName: Raphael Recipe Solver, foodId: 123, foodHq: false, potionId: 456, potionHq: true }
missionConfig:
  374:
    enabled: true
    gProfileId: 2
    turninSilver: true
    craftSettings:
      999: { craftProfileId: 7 }
  375: { enabled: true, manualMode: true }
cosmicAgenda:
  - { goal: RelicStage, job: 16, target: 12 }
  - { goal: ClassLevel, job: 17, target: 90 }
""";
using var document = System.Text.Json.JsonDocument.Parse(ConvertYaml(yaml));
var cfg = document.RootElement;
Check(cfg.GetProperty("SelectedMode").GetInt32() == 10, "Agenda mode not migrated");
Check(!cfg.GetProperty("StartUponEnterMoon").GetBoolean(), "Import enabled unattended start");
Check(cfg.GetProperty("OnlyGrabMission_Debug").GetBoolean(), "Renamed option lost");
var mission = cfg.GetProperty("MissionConfig").GetProperty("374");
Check(mission.GetProperty("GProfileId").GetInt32() == 2 && mission.GetProperty("TurninGoal").GetInt32() == 2, "Mission settings changed semantics");
Check(!cfg.GetProperty("MissionConfig").GetProperty("375").GetProperty("Enabled").GetBoolean(), "Manual mission became automatic");
var craft = mission.GetProperty("CraftSettings").GetProperty("999");
Check(craft.GetProperty("SolverOverrideName").GetString() == "Raphael Recipe Solver" && !craft.GetProperty("UseGlobal").GetBoolean(), "Craft profile not resolved");
Check(craft.GetProperty("FoodId").GetUInt32() == 123 && !craft.GetProperty("FoodHQ").GetBoolean() && craft.GetProperty("PotionHQ").GetBoolean(), "Consumables changed");
Check(cfg.GetProperty("LegacyImportWarnings").GetArrayLength() == 2, "Manual-mode import warning was not persisted");
Check(cfg.GetProperty("MissionConfig").GetProperty("375").GetProperty("TurninGoal").GetInt32() == 6, "Missing score thresholds became early turn-in");
var entries = cfg.GetProperty("Cosmic_Agenda");
Check(entries[0].GetProperty("SelectedOption").GetInt32() == 5 && entries[0].GetProperty("SelectedRelicLevel").GetInt32() == 12, "Relic target lost");
Check(entries[1].GetProperty("SelectedOption").GetInt32() == 9 && entries[1].GetProperty("ClassLevel").GetInt32() == 90, "Level target lost");
foreach (var entryCase in new[]{("CosmoCredits", 6, "CreditAmount"), ("ClassScore", 10, "ClassScore"), ("LunarCredits", 7, "PlanetAmount"), ("StandardMissionsGolded", 11, "ClassScore")})
{
    using var test = System.Text.Json.JsonDocument.Parse(ConvertYaml($"cosmicAgenda: [{{goal: {entryCase.Item1}, job: 16, target: 123}}]"));
    var item = test.RootElement.GetProperty("Cosmic_Agenda")[0];
    Check(item.GetProperty("SelectedOption").GetInt32() == entryCase.Item2 && item.GetProperty(entryCase.Item3).GetInt32() == 123, "Agenda goal/target changed");
}
foreach (var invalid in new[]{"cosmicAgenda: [{goal: Unknown, job: 16, target: 1}]", "cosmicAgenda: [{goal: ClassLevel, job: 1, target: 90}]", "cosmicAgenda: [{goal: StandardMissionsGolded, job: 16, target: 1, standardGoldARank: false}]"})
{
    bool failed = false;
    try { ConvertYaml(invalid); } catch (TargetInvocationException) { failed = true; }
    Check(failed, "Unrepresentable agenda was silently changed");
}
var temp = Path.Combine(root, "tests", "migration-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    var source = Path.Combine(temp, "Mission Config.yaml");
    var destination = Path.Combine(temp, "DefaultConfig.json");
    File.WriteAllText(source, yaml);
    var import = importer.GetMethod("TryImport")!;
    Check((bool)import.Invoke(null, new[]{source,destination})!, "First import failed");
    Check(File.ReadAllText(source) == yaml, "Original YAML modified");
    Check(Directory.GetFiles(temp, "Legacy-Mission-Config-*.yaml").Length == 1, "Backup missing");
    var first = File.ReadAllText(destination);
    File.WriteAllText(source, "invalid: [");
    Check(!(bool)import.Invoke(null, new[]{source,destination})! && File.ReadAllText(destination) == first, "Repeated import replaced candidate settings");
    var replace = importer.GetMethod("ReplaceImport")!;
    var hash = (string)importer.GetMethod("SourceHash")!.Invoke(null, new[]{source})!;
    File.WriteAllText(source, yaml);
    bool changed = false;
    try { replace.Invoke(null, new[]{source,destination,hash}); } catch (TargetInvocationException) { changed = true; }
    Check(changed && File.ReadAllText(destination) == first, "Changed source bypassed import preview validation");
    hash = (string)importer.GetMethod("SourceHash")!.Invoke(null, new[]{source})!;
    File.WriteAllText(destination, "previous candidate settings");
    replace.Invoke(null, new[]{source,destination,hash});
    Check(Directory.GetFiles(temp, "*.before-import-*.json").Any(path => File.ReadAllText(path) == "previous candidate settings"), "Candidate backup missing before replacement");
    Check(File.ReadAllText(source) == yaml && File.ReadAllText(destination).Contains("Cosmic_Agenda"), "Replacement did not preserve source or target schema");
    File.WriteAllText(source, "invalid: [");
    File.Delete(destination);
    bool rejected = false;
    try { import.Invoke(null, new[]{source,destination}); } catch (TargetInvocationException) { rejected = true; }
    Check(rejected && !File.Exists(destination), "Malformed YAML left a partial candidate config");
}
finally
{
    foreach (var file in Directory.GetFiles(temp)) File.Delete(file);
    Directory.Delete(temp);
}
Console.WriteLine($"PASS: {checks} API13 IPC, Artisan adapter and real YAML migration checks.");
