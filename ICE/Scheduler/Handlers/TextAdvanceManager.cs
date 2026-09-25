using ECommons.EzSharedDataManager;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers
{
    internal static class TextAdvancedManager
    {
        private static bool WasChanged = false;
        internal static void Tick()
        {
            var currentState = SchedulerMain.State;
            bool shouldDisable = SchedulerMain.State == IceState.GrabMission || SchedulerMain.State == IceState.AbandonMission;

            if (WasChanged)
            {
                if (!shouldDisable)
                {
                    UnlockTA();
                }
            }
            else
            {
                if (shouldDisable)
                {
                    WasChanged = true;
                    LockTA();
                }
            }
        }
        internal static void LockTA()
        {
            if (EzSharedData.TryGet<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Add(Name);
            }
        }

        internal static void UnlockTA()
        {
            if (EzSharedData.TryGet<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Remove(Name);
            }
        }
    }
}

