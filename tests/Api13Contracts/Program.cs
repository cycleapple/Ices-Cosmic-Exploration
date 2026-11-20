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
Console.WriteLine($"PASS: {checks} API13 IPC and actual Artisan adapter transaction checks.");
