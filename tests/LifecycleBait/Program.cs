using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
var pluginPath=Path.GetFullPath(args[0]);
var folder=Path.GetDirectoryName(pluginPath)!;
var sdk=Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving+=(ctx,name)=> {
 foreach(var dir in new[]{folder,sdk}) { var file=Path.Combine(dir,name.Name+".dll"); if(File.Exists(file))return ctx.LoadFromAssemblyPath(file); } return null;
};
var ice=AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
var ec=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(folder,"ECommons.dll"));
var svc=ec.GetType("ECommons.DalamudServices.Svc",true)!;
var log=svc.GetProperty("Log")!;
log.SetValue(null,DispatchProxy.Create(log.PropertyType,typeof(NullService)));
var main=ice.GetType("ICE.ICE",true)!;
var plugin=RuntimeHelpers.GetUninitializedObject(main);
main.GetField("P",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,plugin);
var configField=main.GetField("config",BindingFlags.Instance|BindingFlags.NonPublic);
if(configField!=null) configField.SetValue(plugin,RuntimeHelpers.GetUninitializedObject(configField.FieldType));
else {
 var field=main.GetField("missionConfigs",BindingFlags.Static|BindingFlags.NonPublic)!;
 field.SetValue(null,RuntimeHelpers.GetUninitializedObject(field.FieldType));
}
var turnin=ice.GetType("ICE.Scheduler.Tasks.Task_TurninMission",true)!;
var settings=ice.GetType("ICE.Utilities.Mission_Settings",true)!;
var scheduler=ice.GetType("ICE.Scheduler.SchedulerMain",true)!;
var state=scheduler.GetField("State",BindingFlags.NonPublic|BindingFlags.Static)!;
var stop=settings.GetField("StopAfterCurrent",BindingFlags.NonPublic|BindingFlags.Static)!;
int checks=0;
void Check(bool pass,string why){checks++;if(!pass)throw new Exception(why);}
Check(ice.GetReferencedAssemblies().Single(x=>x.Name=="Dalamud").Version!.Major==13,"API13 dependency");
Check(ice.GetReferencedAssemblies().Single(x=>x.Name=="System.Runtime").Version!.Major==9,"NET9 dependency");
turnin.GetField("PreviousMissionId")!.SetValue(null,(uint)0);
for(int i=0;i<3;i++) {
 stop.SetValue(null,false);
 Check((bool)turnin.GetMethod("GoldCheck")!.Invoke(null,null)!,"Zero-ID bookkeeping must finish");
 Check(state.GetValue(null)!.ToString()=="Start","Missing previous mission must continue to next mission");
}
stop.SetValue(null,true);
Check((bool)turnin.GetMethod("GoldCheck")!.Invoke(null,null)!,"Zero-ID stop-after-current must finish");
Check(state.GetValue(null)!.ToString()=="Idle","Stop-after-current must remain respected");
Check(!(bool)stop.GetValue(null)!,"Stop flag resets after completion");
Check((bool)turnin.GetMethod("JobSwapCheck")!.Invoke(null,null)!,"Unknown previous mission must not break job check");
var select=ice.GetType("ICE.Utilities.FishingBaitPolicy",true)!.GetMethod("Select")!;
uint Select(uint current,params uint[] owned)=>(uint)select.Invoke(null,new object[]{current,owned})!;
Check(Select(0,47686)==47686,"MF-only can pick first owned cosmic bait");
Check(Select(47686,47686,999)==47686,"Preserve selected owned bait");
Check(Select(999,47686)==47686,"Replace exhausted/unsuitable selected bait");
Check(Select(0)==0,"No owned bait does not invent a bait");
Check(Select(0,0,47686)==47686,"Ignore zero IDs");
Check(Select(999,47686,999)==999,"Preserve non-first suitable bait");
Console.WriteLine($"PASS: {checks} compiled lifecycle/bait checks for {ice.GetName().Name}");
public class NullService:DispatchProxy {
 protected override object? Invoke(MethodInfo? method, object?[]? args)=>method!.ReturnType==typeof(void)?null:method.ReturnType.IsValueType?Activator.CreateInstance(method.ReturnType):null;
}
