using ECommons.GameHelpers;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks;

// Chooses the shortest available cosmic-exploration travel method.  Every optional
// method has a direct-walk fallback, so an unavailable teleport never blocks a task.
internal static class CosmicTravelPlanner
{
    private enum TravelMethod { Direct, HubReturn, RedAlert, HubRedAlert }

    private sealed class TravelPlan
    {
        public required Vector3 Destination { get; init; }
        public required uint Territory { get; init; }
        public uint MissionId { get; init; }
        public bool WaitForBusy { get; init; }
        public float Distance { get; init; }
        public bool StayMounted { get; init; }
        public Task<TravelMethod> ChoiceTask { get; set; } = null!;
        public TravelMethod? Method { get; set; }
        public RedAlertNpc? RedAlertNpc { get; set; }
        public int RedAlertSelection { get; set; }
        public Vector3 RedAlertExit { get; set; }
        public int Stage { get; set; }
        public bool? UsingCosmoliner { get; set; }
        public DateTime StageStarted { get; set; } = DateTime.UtcNow;
    }

    private sealed record RedAlertNpc(uint ObjectId, Vector3 Location);

    private static readonly Dictionary<uint, Vector3> HubCenters = new()
    {
        [1237] = new(2.84f, 1.55f, -0.06f),
        [1291] = new(339.90f, 52.60f, -412.10f),
    };

    private static readonly Dictionary<uint, RedAlertNpc> RedAlertNpcs = new()
    {
        [1237] = new(1052663, new(15.28f, 1.64f, -3.83f)),
        [1291] = new(1052626, new(343.46f, 52.64f, -441.80f)),
    };

    private static readonly Dictionary<uint, int> RedAlertSelections = BuildRedAlertSelections();
    private static TravelPlan? currentPlan;

    public static bool Move(Vector3 destination, bool waitForBusy, float distance, bool stayMounted, uint missionId = 0)
    {
        if (!P.Navmesh.Installed)
            return true;

        missionId = missionId == 0 ? CosmicHelper.CurrentLunarMission : missionId;

        if (Player.DistanceTo(destination) <= distance)
        {
            Reset();
            return true;
        }

        var territory = Player.Territory;
        if (currentPlan == null
            || currentPlan.Territory != territory
            || currentPlan.MissionId != missionId
            || Vector3.DistanceSquared(currentPlan.Destination, destination) > 1f)
        {
            var allowHubReturn = currentPlan == null;
            Reset();
            currentPlan = CreatePlan(destination, territory, waitForBusy, distance, stayMounted, missionId, allowHubReturn);
            return false;
        }

        if (currentPlan.Method == null)
        {
            if (!currentPlan.ChoiceTask.IsCompleted)
                return false;

            currentPlan.Method = currentPlan.ChoiceTask.Status == TaskStatus.RanToCompletion
                ? currentPlan.ChoiceTask.Result
                : TravelMethod.Direct;
            IceLogging.Info($"Cosmic travel selected: {currentPlan.Method}", "[Travel]");
        }

        if (((currentPlan.Method is TravelMethod.HubReturn or TravelMethod.HubRedAlert) && !C.UseHubReturn)
            || ((currentPlan.Method is TravelMethod.RedAlert or TravelMethod.HubRedAlert) && !C.UseRedAlertNpc))
            SwitchToDirect(currentPlan);

        return ExecutePlan(currentPlan);
    }

    private static TravelPlan CreatePlan(Vector3 destination, uint territory, bool waitForBusy, float distance, bool stayMounted, uint missionId, bool allowHubReturn)
    {
        var plan = new TravelPlan
        {
            Destination = destination,
            Territory = territory,
            MissionId = missionId,
            WaitForBusy = waitForBusy,
            Distance = distance,
            StayMounted = stayMounted,
            ChoiceTask = null!,
        };
        plan.ChoiceTask = ChooseTravel(plan, Player.Position, allowHubReturn);
        return plan;
    }

    private static async Task<TravelMethod> ChooseTravel(TravelPlan plan, Vector3 playerPosition, bool allowHubReturn)
    {
        var candidates = new List<(TravelMethod Method, float Distance)> { (TravelMethod.Direct, await PathCost(playerPosition, plan.Destination)) };
        HubCenters.TryGetValue(plan.Territory, out var hub);

        if (allowHubReturn && C.UseHubReturn && hub != Vector3.Zero && Vector3.Distance(playerPosition, hub) > 75f)
            candidates.Add((TravelMethod.HubReturn, await PathCost(hub, plan.Destination) * 1.2f));

        if (C.UseRedAlertNpc && plan.MissionId != 0)
        {
            var hasNpc = RedAlertNpcs.TryGetValue(plan.Territory, out var redNpc);
            var hasSelection = RedAlertSelections.TryGetValue(plan.MissionId, out var selection);
            var hasExit = GatheringUtil.CriticalLocations.TryGetValue(plan.MissionId, out var criticalLocation);
            if (!hasNpc || !hasSelection || !hasExit)
            {
                IceLogging.Info($"Mission {plan.MissionId}: RedAlert unavailable; NPC={hasNpc}, selection={hasSelection}, exit={hasExit}.", "[TravelProbe]");
            }
            else
            {
            plan.RedAlertNpc = redNpc;
            plan.RedAlertSelection = selection;
            plan.RedAlertExit = criticalLocation.RawLocation;
            IceLogging.Info($"Mission {plan.MissionId}: RedAlert data: territory {plan.Territory}, NPC base {redNpc.ObjectId} at {redNpc.Location}, " +
                $"selection {selection}, expected exit {plan.RedAlertExit}, destination {plan.Destination}.", "[TravelProbe]");
            var redAlertExitDistance = await PathCost(criticalLocation.RawLocation, plan.Destination);
            candidates.Add((TravelMethod.RedAlert, await PathCost(playerPosition, redNpc.Location) + redAlertExitDistance));
            if (allowHubReturn && C.UseHubReturn && hub != Vector3.Zero && Vector3.Distance(playerPosition, hub) > 75f)
                candidates.Add((TravelMethod.HubRedAlert, await PathCost(hub, redNpc.Location) + redAlertExitDistance));
            }
        }

        var choice = candidates.Where(x => !float.IsInfinity(x.Distance) && !float.IsNaN(x.Distance))
            .OrderBy(x => x.Distance)
            .Select(x => x.Method)
            .FirstOrDefault(TravelMethod.Direct);
        IceLogging.Info($"Mission {plan.MissionId}, from {playerPosition} to {plan.Destination}; " +
            $"{string.Join(", ", candidates.Select(x => $"{x.Method}Cost={x.Distance:F0}"))}; selected {choice}", "[TravelProbe]");
        return choice;
    }

    private static async Task<float> PathCost(Vector3 from, Vector3 to)
    {
        try
        {
            var score = await P.Navmesh.PathfindScore(from, to, false).ConfigureAwait(false);
            if (!float.IsInfinity(score) && !float.IsNaN(score))
                return score;
        }
        catch { }

        try
        {
            var path = await P.Navmesh.Pathfind(from, to, false).ConfigureAwait(false);
            return path == null || path.Count < 2
                ? Vector3.Distance(from, to)
                : path.Zip(path.Skip(1), Vector3.Distance).Sum();
        }
        catch { return float.PositiveInfinity; }
    }

    private static bool ExecutePlan(TravelPlan plan)
    {
        if (plan.Method != TravelMethod.Direct && StageTimedOut(plan))
        {
            IceLogging.Warning("Cosmic travel stage timed out; falling back to direct pathing.", "[Travel]");
            SwitchToDirect(plan);
        }

        switch (plan.Method)
        {
            case TravelMethod.HubReturn:
                return ReturnToHubThenDirect(plan);
            case TravelMethod.RedAlert:
                return RedAlertThenDirect(plan);
            case TravelMethod.HubRedAlert:
                return HubThenRedAlertThenDirect(plan);
            default:
                return Direct(plan);
        }
    }

    private static bool Direct(TravelPlan plan)
    {
        var usingCosmoliner = Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Unknown101];
        if (plan.UsingCosmoliner != usingCosmoliner)
        {
            plan.UsingCosmoliner = usingCosmoliner;
            IceLogging.Info($"Mission {plan.MissionId}: Cosmoliner {(usingCosmoliner ? "entered" : "exited")} at {Player.Position}", "[TravelProbe]");
        }
        if (!Task_NavmeshMove.NavToDestinationDirect(plan.Destination, plan.WaitForBusy, plan.Distance, plan.StayMounted))
            return false;
        Reset();
        return true;
    }

    private static bool ReturnToHubThenDirect(TravelPlan plan)
    {
        if (plan.Stage == 0)
        {
            if (!ReturnToHub(plan))
                return false;
            AdvanceStage(plan);
        }
        if (!TravelTransitionSettled())
            return false;
        return Direct(plan);
    }

    private static bool RedAlertThenDirect(TravelPlan plan)
    {
        if (plan.RedAlertNpc == null)
            return Direct(plan);
        if (plan.Stage == 0 && !MoveTo(plan, plan.RedAlertNpc.Location, 4f))
            return false;
        if (plan.Stage == 0)
            AdvanceStage(plan);
        if (plan.Stage == 1 && !TravelRedAlert(plan))
            return false;
        if (plan.Stage == 1)
            AdvanceStage(plan);
        return Direct(plan);
    }

    private static bool HubThenRedAlertThenDirect(TravelPlan plan)
    {
        if (plan.Stage == 0)
        {
            if (!ReturnToHub(plan))
                return false;
            AdvanceStage(plan);
        }
        if (!TravelTransitionSettled())
            return false;
        if (plan.Stage == 1)
        {
            plan.Stage = 0;
            plan.Method = TravelMethod.RedAlert;
            plan.StageStarted = DateTime.UtcNow;
        }
        return RedAlertThenDirect(plan);
    }

    private static bool MoveTo(TravelPlan plan, Vector3 point, float distance)
        => Task_NavmeshMove.NavToDestinationDirect(point, true, distance, false);

    private static unsafe bool ReturnToHub(TravelPlan plan)
    {
        if (!HubCenters.TryGetValue(plan.Territory, out var hub))
            return true;
        if (Player.DistanceTo(hub) <= 45f)
            return true;
        if (!Player.IsBusy && EzThrottler.Throttle("Cosmic travel hub return", 1000))
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 26);
        return false;
    }

    private static bool TravelTransitionSettled()
        => PlayerHelper.IsScreenReady() && !PlayerHelper.CustomIsBusy;

    private static bool TravelRedAlert(TravelPlan plan)
    {
        if (plan.RedAlertNpc == null)
            return true;
        if (Player.DistanceTo(plan.Destination) <= 90f && Player.DistanceTo(plan.RedAlertNpc.Location) > 25f && TravelTransitionSettled())
        {
            IceLogging.Info($"Mission {plan.MissionId}: RedAlert landed at {Player.Position}; expected exit {plan.RedAlertExit}, destination {plan.Destination}.", "[TravelProbe]");
            return true;
        }

        if (GenericHelpers.TryGetAddonMaster<SelectString>(out var select) && select.IsAddonReady)
        {
            if (plan.RedAlertSelection >= 0 && plan.RedAlertSelection < select.Entries.Count() && EzThrottler.Throttle("Cosmic travel red alert select", 500))
            {
                IceLogging.Info($"Mission {plan.MissionId}: RedAlert dialog has {select.Entries.Count()} entries; selecting index {plan.RedAlertSelection}.", "[TravelProbe]");
                select.Entries[plan.RedAlertSelection].Select();
            }
            else if (plan.RedAlertSelection < 0 || plan.RedAlertSelection >= select.Entries.Count())
            {
                if (EzThrottler.Throttle("Cosmic travel red alert invalid selection", 3000))
                    IceLogging.Warning($"Mission {plan.MissionId}: RedAlert selection index {plan.RedAlertSelection} is outside dialog entry count {select.Entries.Count()}.", "[TravelProbe]");
            }
            return false;
        }
        if (GenericHelpers.TryGetAddonMaster<SelectYesno>(out var yesNo) && yesNo.IsAddonReady)
        {
            if (EzThrottler.Throttle("Cosmic travel red alert confirm", 500))
                yesNo.Yes();
            return false;
        }
        if (GenericHelpers.TryGetAddonMaster<Talk>(out var talk) && talk.IsAddonReady)
        {
            if (EzThrottler.Throttle("Cosmic travel red alert talk", 500))
                talk.Click();
            return false;
        }

        var npc = Svc.Objects.FirstOrDefault(x => x.DataId == plan.RedAlertNpc.ObjectId);
        if (npc == null)
        {
            if (EzThrottler.Throttle("Cosmic travel red alert NPC missing", 5000))
                IceLogging.Warning($"Mission {plan.MissionId}: RedAlert NPC base {plan.RedAlertNpc.ObjectId} was not found; expected {plan.RedAlertNpc.Location}, player {Player.Position}.", "[TravelProbe]");
            return false;
        }
        if (Player.Mounted || Player.IsJumping)
        {
            Utils.Dismount();
            return false;
        }
        if (!Player.IsBusy && EzThrottler.Throttle("Cosmic travel red alert interact", 1000))
        {
            IceLogging.Info($"Mission {plan.MissionId}: interacting with RedAlert NPC base {npc.DataId} at {npc.Position}; expected {plan.RedAlertNpc.Location}.", "[TravelProbe]");
            Utils.TargetgameObject(npc);
            Utils.InteractWithObject(npc);
        }
        return false;
    }

    private static bool StageTimedOut(TravelPlan plan) => DateTime.UtcNow - plan.StageStarted > TimeSpan.FromSeconds(30);
    private static void AdvanceStage(TravelPlan plan) { plan.Stage++; plan.StageStarted = DateTime.UtcNow; }
    private static void SwitchToDirect(TravelPlan plan)
    {
        if (plan.Method != TravelMethod.Direct && P.Navmesh.IsRunning())
            P.Navmesh.Stop();
        plan.Method = TravelMethod.Direct;
        plan.Stage = 0;
        plan.StageStarted = DateTime.UtcNow;
    }
    private static void Reset() => currentPlan = null;

    private static Dictionary<uint, int> BuildRedAlertSelections()
    {
        var result = new Dictionary<uint, int>();
        Add(result, 0, 518, 522, 530, 512, 521, 527, 515, 524, 538, 516, 520, 525, 517, 532, 513, 526, 528);
        Add(result, 1, 537, 543, 533, 536, 542, 519, 523, 531, 534, 539, 514, 529, 541, 535, 540, 544);
        Add(result, 0, 1007, 1019, 1025, 1017, 1023, 1034, 1028, 1032, 1037, 1013, 1029, 1038, 1009, 1014, 1021, 1015, 1018, 1033);
        Add(result, 1, 1016, 1022, 1010, 1026, 1031, 1011, 1020, 1035, 1008, 1036, 1030, 1039, 1012, 1024, 1027);
        return result;
    }

    private static void Add(Dictionary<uint, int> target, int selection, params uint[] missionIds)
    {
        foreach (var missionId in missionIds)
            target[missionId] = selection;
    }
}
