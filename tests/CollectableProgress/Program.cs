using ICE.Utilities.GatheringHelper;

var context = new CollectableProgressWatchdog.Context(374, 1, 100);
var progress = new CollectableProgressWatchdog.Progress(200, 4, 600, 0, false);
var watchdog = new CollectableProgressWatchdog();
int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
bool Observe(long now, bool canAct = true) => watchdog.Observe(context, progress, canAct, now);
void IdleUntil(long start, long end)
{
    for (long now = start; now < end; now += 500)
        Check(!Observe(now), "Recovered before five actionable seconds");
}

IdleUntil(0, 5000);
Check(Observe(5000), "Did not recover from a continuously stalled rotation");
progress = progress with { Integrity = 3 };
watchdog.Suspend();
Check(Observe(5500), "Collection animation/progress restarted a stalled rotation");

foreach (var nextContext in new[] { context with { Mission = 375 }, context with { Node = 2 }, context with { Item = 101 } })
    Check(!watchdog.Observe(nextContext, progress, true, 6000), "Recovery leaked across mission/node/item");

watchdog.Reset();
IdleUntil(10000, 14500);
Check(!Observe(14500, false), "Animation counted as actionable time");
IdleUntil(15000, 20000);
Check(Observe(20000), "Did not restart idle budget after animation");

watchdog.Reset();
IdleUntil(30000, 34500);
Check(!Observe(60000), "Paused scheduler consumed idle budget");
IdleUntil(60500, 65000);
Check(Observe(65000), "Did not recover after resuming continuous observations");

foreach (var changed in new[] {
    progress with { Quality = 300 }, progress with { Integrity = 2 },
    progress with { Gp = 400 }, progress with { Step = 1 }, progress with { HighStandard = true } })
{
    watchdog.Reset();
    IdleUntil(70000, 74500);
    Check(!watchdog.Observe(context, changed, true, 74500), "Progress did not reset idle budget");
    Check(!watchdog.Observe(context, changed, true, 75000), "Recovered immediately after progress");
}
watchdog.Reset();
Check(!Observe(80000), "Closed/reopened window inherited recovery");
watchdog.Reset();
for (long now = 90000; now < 95000; now += 500)
{
    progress = progress with { Gp = progress.Gp + 5 };
    Check(!Observe(now), "Recovered early during passive GP regeneration");
}
Check(Observe(95000), "Passive GP regeneration hid a stalled rotation");
Console.WriteLine($"PASS: {checks} checks covering idle timeout, animations, pause/resume, progress and context resets.");
