using System;
namespace ICE.Compat;

public enum CraftRequestProgress { NotSent, Waiting, Running, Completed, Failed, TimedOut }
public sealed class ArtisanCraftRequest
{
    public bool Sent { get; private set; }
    public bool Accepted { get; private set; }
    private long sentAt;
    public void Reset() { Sent = false; Accepted = false; sentAt = 0; }
    public bool Send(Action dispatch, long now)
    {
        if (Sent) return false;
        dispatch(); // CraftX clears the provider's previous request failure synchronously.
        Sent = true;
        sentAt = now;
        return true;
    }
    public CraftRequestProgress Observe(long now, Func<int> status, Func<bool> busy)
    {
        if (!Sent) return CraftRequestProgress.NotSent; // Never read a previous request's failure.
        if (status() == 2) return CraftRequestProgress.Failed;
        if (busy()) { Accepted = true; return CraftRequestProgress.Running; }
        if (Accepted) return CraftRequestProgress.Completed;
        return now - sentAt >= 15000 ? CraftRequestProgress.TimedOut : CraftRequestProgress.Waiting;
    }
}
