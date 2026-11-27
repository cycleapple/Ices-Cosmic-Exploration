using ICE.Compat;
using ECommons.UIHelpers.AddonMasterImplementations;
int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
foreach (var text in new[]{"500", " 500 ", "分數：500", "500 分"}) Check(MissionScoreText.Parse(text)==500, text);
Check(MissionScoreText.Parse("1,500")==1500, "English grouping");
Check(MissionScoreText.Parse("1，500")==1500, "TC grouping");
foreach (var text in new string?[]{null,"", "—", "350 / 500", "350／500", "分數 350 目標 500", "1,50", "4294967296"}) Check(MissionScoreText.Parse(text)==null, "Ambiguous score: "+text);
foreach (uint threshold in new uint[]{100,500,1500,65536,100000}) {
 Check(MissionScorePolicy.Classify(threshold-1,1,50,threshold,true)!=MissionRank.Gold,"Below gold must continue");
 Check(MissionScorePolicy.Classify(threshold,1,50,threshold,true)==MissionRank.Gold,"Exact gold must turn in");
 Check(MissionScorePolicy.Classify(threshold+1,1,50,threshold,true)==MissionRank.Gold,"Above gold must turn in");
}
Check(MissionScorePolicy.Classify(null,100,200,500,true)==MissionRank.None,"Missing HUD is not a medal");
Check(MissionScorePolicy.Classify(0,0,0,0,false)==MissionRank.None,"Zero thresholds are not gold");
Check(MissionScorePolicy.Classify(0,0,null,null,true)==MissionRank.Bronze,"Enabled report gives only bronze");
Check(MissionScorePolicy.Classify(200,100,200,500,true)==MissionRank.Silver,"Report enabled is not gold");
Check(MissionScorePolicy.Classify(99,100,200,500,false)==MissionRank.None,"Below bronze");
Check(!RepairEligibility.NeedsRepair(0,true,0,50),"Empty slot cannot need repairs");
Check(!RepairEligibility.NeedsRepair(1,false,0,50),"Soul crystal cannot need repairs");
Check(RepairEligibility.NeedsRepair(1,true,11100,50),"37 percent damaged gear");
Check(RepairEligibility.NeedsRepair(1,true,15000,50),"Exact repair threshold");
Check(!RepairEligibility.NeedsRepair(1,true,15001,50),"Above repair threshold");
Check(!RepairEligibility.NeedsRepair(1,true,60000,99.9f),"Overrepair is healthy");
Check(!RepairEligibility.NeedsRepair(1,true,0,float.NaN),"Invalid threshold");
var sweep=new RepairSweepTracker();
Check(!sweep.IsStalled(1,100),"First observation waits");
Check(!sweep.IsStalled(1,10099),"Less than 10 seconds waits");
Check(sweep.IsStalled(1,10100),"Unchanged condition stops retrying");
Check(!sweep.IsStalled(2,10101),"Actual repair resets watchdog");
Check(!sweep.IsStalled(2,20100),"Progress gets full grace period");
Check(sweep.IsStalled(2,20101),"Progress eventually stalls");
for(int i=0;i<6;i++) Check(sweep.Advance(),"Visit remaining category");
Check(!sweep.Advance(),"Seven categories bound the sweep");
sweep.Reset();
Check(sweep.Advance(),"New repair run resets categories");
Check(!sweep.IsStalled(2,999999),"New run resets timer");
Console.WriteLine($"Mission/repair regression: {checks} checks passed");
