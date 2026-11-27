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
if (ice.GetType("ICE.Compat.RelicTurninPolicy") is { } relicPolicy)
{
 var selectedJob=settings.GetField("SelectedJob",BindingFlags.NonPublic|BindingFlags.Static)!;
 selectedJob.SetValue(null,(uint)18);
 var relicTask=ice.GetType("ICE.Scheduler.Tasks.Task_RelicTurnin",true)!;
 Check((bool)relicTask.GetMethod("RegisterJob")!.Invoke(null,null)!,"Record tool target before job swap");
 Check((uint)relicTask.GetField("TurninJob")!.GetValue(null)! ==18,"Turn in the selected fisher research, not a temporary battle job");
 var next=relicPolicy.GetMethod("NextStage")!;
 Check((int)next.Invoke(null,new object[]{2,new int[]{3450,3400,3250,2250,100}})! ==3,"API13 positive analysis needs define next stage");
 Check((int)next.Invoke(null,new object[]{9,new int[]{3450}})! ==10,"Nominal moon cap cannot mask pending analysis");
 Check((int)next.Invoke(null,new object[]{9,new int[]{0,0}})! ==9,"No pending analysis is a completed stage");
 Check((int)next.Invoke(null,new object[]{0,Array.Empty<int>()})! ==0,"Unloaded data cannot invent another stage");
 var decide=relicPolicy.GetMethod("ShouldTurnin")!;
 bool Relic(int stage,int unlocked,int[] current,int[] needed,bool auto,int target)
     =>(bool)decide.Invoke(null,new object[]{stage,unlocked,current,needed,auto,target})!;
 int[] current=[3649,3424,3362,2424,100], needed=[3450,3400,3250,2250,100];
 Check(Relic(2,3,current,needed,true,0),"Screenshot data meets analysis requirements before inventory cap");
 Check(Relic(2,3,current,needed,false,14),"Agenda tool-stage goal must turn in without general auto toggle");
 Check(Relic(2,3,current,needed,false,int.MaxValue),"Tool-exp goal advances unlocked stages");
 Check(!Relic(2,3,current,needed,false,0),"Standard manual turn-in preference preserved");
 Check(!Relic(2,3,current,needed,false,2),"Already reached agenda stage does not force another turn-in");
 Check(!Relic(3,3,current,needed,true,14),"Unavailable next stage cannot be reported");
 Check(!Relic(4,3,current,needed,true,14),"Stale unlocked stage is not an upgrade");
 Check(!Relic(2,3,[3649,3424,3362,2249,100],needed,true,0),"One incomplete requirement must continue gathering");
 Check(Relic(2,3,needed,needed,true,0),"Exact needed values suffice without max overflow");
 Check(!Relic(2,3,[],[],true,14),"Unloaded analysis never appears complete");
 Check(!Relic(2,3,[0,0],[0,0],true,14),"No positive requirements is not a turn-in");
 Check(!Relic(2,3,[10],[10,10],true,14),"Incomplete snapshot rejected");
 Check(!Relic(2,3,[-1],[10],true,14),"Negative snapshot rejected");
 Check(!Relic(2,3,[10],[-1],true,14),"Invalid requirement rejected");
 Check(Relic(2,3,[10,0],[10,0],false,3),"Unused analysis types do not block a real requirement");
 Check(!Relic(-1,3,current,needed,true,14),"Invalid stage rejected");
}
Console.WriteLine($"PASS: {checks} compiled lifecycle/bait/relic checks for {ice.GetName().Name}");
public class NullService:DispatchProxy {
 protected override object? Invoke(MethodInfo? method, object?[]? args)=>method!.ReturnType==typeof(void)?null:method.ReturnType.IsValueType?Activator.CreateInstance(method.ReturnType):null;
}
