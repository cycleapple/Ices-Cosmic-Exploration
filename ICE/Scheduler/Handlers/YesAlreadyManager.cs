using ECommons.EzSharedDataManager;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers
{
    internal static class YesAlreadyManager
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
                    WasChanged = false;
                    Unlock();
                }
            }
            else
            {
                if (shouldDisable)
                {
                    WasChanged = true;
                    Lock();
                }
            }
        }
        internal static void Lock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Add(Name);
            }
        }

        internal static void Unlock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Remove(Name);
            }
        }
    }
}
