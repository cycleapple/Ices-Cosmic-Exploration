namespace ICE.Utilities.GatheringHelper;

// Counts continuous actionable idle time, rather than time spent in animations or paused.
internal sealed class CollectableProgressWatchdog
{
    internal readonly record struct Context(uint Mission, uint Node, uint Item);
    internal readonly record struct Progress(int Quality, uint Integrity, int Gp, int Step, bool HighStandard);

    private Context? context;
    private Progress progress;
    private long? idleSince;
    private long lastObserved;
    private bool recovering;

    internal bool Observe(Context currentContext, Progress currentProgress, bool canAct, long now)
    {
        if (context != currentContext)
        {
            Reset();
            context = currentContext;
        }

        // A scheduler pause or a long gap between frames must not consume the idle budget.
        // Passive GP regeneration is not evidence that a gathering action succeeded.
        bool madeProgress = (currentProgress with { Gp = progress.Gp }) != progress || currentProgress.Gp < progress.Gp;
        if (!canAct || madeProgress || now - lastObserved > 1000 || now < lastObserved)
            idleSince = null;

        progress = currentProgress;
        lastObserved = now;
        if (canAct)
        {
            idleSince ??= now;
            recovering |= now - idleSince.Value >= 5000;
        }

        return recovering;
    }

    internal void Suspend() => idleSince = null;

    internal void Reset()
    {
        context = null;
        progress = default;
        idleSince = null;
        lastObserved = 0;
        recovering = false;
    }
}
