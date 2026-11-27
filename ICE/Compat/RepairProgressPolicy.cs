namespace ICE.Compat;

public static class RepairEligibility
{
    public static bool NeedsRepair(uint itemId, bool durable, uint condition, float threshold)
        => itemId != 0 && durable && threshold >= 0 && threshold <= 100 && condition <= threshold * 300;
}

public sealed class RepairSweepTracker
{
    private int sections;
    private ulong? signature;
    private long changedAt;
    public void Reset() { sections = 0; signature = null; changedAt = 0; }
    public bool IsStalled(ulong current, long now)
    {
        if (signature != current) { signature = current; changedAt = now; }
        return now - changedAt >= 10000;
    }
    public bool Advance() { signature = null; return ++sections < 7; }
}
