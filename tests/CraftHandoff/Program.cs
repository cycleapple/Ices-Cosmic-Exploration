using System.Reflection;
using System.Runtime.Loader;
var icePath=Path.GetFullPath(args[0]); var artisanPath=Path.GetFullPath(args[1]); var sdk=Path.GetFullPath(args[2]);
AssemblyLoadContext.Default.Resolving+=(ctx,name)=> {
 foreach(var dir in new[]{Path.GetDirectoryName(icePath)!,Path.GetDirectoryName(artisanPath)!,sdk}) {
  var path=Path.Combine(dir,name.Name+".dll"); if(File.Exists(path)) return ctx.LoadFromAssemblyPath(path);
 }return null;
};
var ice=AssemblyLoadContext.Default.LoadFromAssemblyPath(icePath);
var artisan=AssemblyLoadContext.Default.LoadFromAssemblyPath(artisanPath);
int checks=0; void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
foreach(var asm in new[]{ice,artisan}) {
 Check(asm.GetReferencedAssemblies().Single(x=>x.Name=="Dalamud").Version!.Major==13,"API13 required");
 Check(asm.GetReferencedAssemblies().Single(x=>x.Name=="System.Runtime").Version!.Major==9,"NET9 required");
}
var requestType=ice.GetType("ICE.Compat.ArtisanCraftRequest",true)!;
var request=Activator.CreateInstance(requestType)!;
int status=2,statusReads=0,busyReads=0,dispatches=0; bool busy=false;
Func<int> readStatus=()=>{statusReads++;return status;}; Func<bool> readBusy=()=>{busyReads++;return busy;};
string Observe(long now)=>requestType.GetMethod("Observe")!.Invoke(request,new object[]{now,readStatus,readBusy})!.ToString()!;
bool Send(long now)=>(bool)requestType.GetMethod("Send")!.Invoke(request,new object[]{(Action)(()=>{dispatches++;status=0;}),now})!;
void Reset()=>requestType.GetMethod("Reset")!.Invoke(request,null);
Check(Observe(0)=="NotSent","Stale previous failure must not stop a new attempt");
Check(statusReads==0&&busyReads==0,"Must not query prior request status before dispatch");
Check(Send(100),"First attempt dispatches");
Check(dispatches==1&&status==0,"New provider request clears previous transport failure");
Check(!Send(200)&&dispatches==1,"Pending request sends exactly once");
Check(Observe(101)=="Waiting","No acknowledgment cannot be treated as success");
busy=true;Check(Observe(102)=="Running","Busy acknowledges this request");
status=2;Check(Observe(103)=="Failed","Failure while waiting must be detected");
status=0;busy=false;Check(Observe(104)=="Completed","Accepted request completes only after idle");
Reset();status=2;Check(Observe(200)=="NotSent","Restart ignores retained failure");
Check(Send(300)&&dispatches==2,"Explicit new run may retry");
Check(Observe(15299)=="Waiting","Allow full acknowledgment timeout");
Check(Observe(15300)=="TimedOut","Missing IPC acknowledgment must stop");
Reset();Check(Send(20000),"New context resets acknowledgment clock");
status=1;busy=true;Check(Observe(90000)=="Running","Long live Raphael generation is not an acknowledgment timeout");
status=2;Check(Observe(90001)=="Failed","A live solver failure remains fatal");
var gateType=artisan.GetType("Artisan.GameInterop.CosmicCraftSelectionGate",true)!;
object Gate(long now=0)=>Activator.CreateInstance(gateType,new object[]{(ushort)36261,now,15000})!;
var gate=Gate();int ensures=0,starts=0,timeouts=0;
bool Tick(object value,long now,bool pending,bool idle,bool visible,ushort selected)=>(bool)gateType.GetMethod("Tick")!.Invoke(value,new object[]{now,pending,idle,visible,selected,(Action)(()=>ensures++),(Action)(()=>starts++),(Action)(()=>timeouts++)})!;
Check(!Tick(gate,0,false,true,true,36262),"Wrong recipe with empty preparation queue must be reselected");
Check(ensures==1&&starts==0,"Never start mismatched recipe 36262");
Check(!Tick(gate,100,true,true,true,36261),"Outstanding preparation must finish");
Check(!Tick(gate,200,false,false,true,36261),"Non-idle craft state is not recipe selection success");
Check(!Tick(gate,300,false,true,false,36261),"Closed cosmic notebook cannot start a craft");
Check(starts==0&&timeouts==0,"No premature start or failure");
Check(Tick(gate,400,false,true,true,36261),"Confirmed matching idle cosmic notebook starts");
Check(starts==1&&timeouts==0,"Exactly one start");
Check(Tick(gate,500,false,true,true,36261)&&starts==1,"Repeated polling cannot start twice");
var changed=Gate(1000);
Check(!Tick(changed,1001,true,true,true,36261),"Selection awaiting preparation");
Check(!Tick(changed,1002,false,true,true,11111),"UI changed after preparation: revalidate, do not start");
Check(starts==1,"Empty queue is not a substitute for matching selection");
Check(Tick(changed,1003,false,true,true,36261)&&starts==2,"Recover correct recipe after UI changes");
var expired=Gate(2000);
Check(!Tick(expired,16999,false,true,true,11111),"Retry mismatched selection before deadline");
Check(Tick(expired,17000,false,true,true,11111),"Persistent mismatch times out");
Check(starts==2&&timeouts==1,"Timeout never starts the wrong recipe");
Check(Tick(expired,18000,false,true,true,36261)&&starts==2&&timeouts==1,"Expired request cannot revive");
var retry=Gate(20000);
Check(Tick(retry,20001,false,true,true,36261)&&starts==3,"New request can retry after a timeout");
bool invalid=false;try{Activator.CreateInstance(gateType,new object[]{(ushort)0,0L,15000});}catch(TargetInvocationException e)when(e.InnerException is ArgumentOutOfRangeException){invalid=true;}
Check(invalid,"Never start an invalid zero recipe");
Console.WriteLine($"PASS: {checks} compiled ICE/Artisan craft handoff regression checks");
