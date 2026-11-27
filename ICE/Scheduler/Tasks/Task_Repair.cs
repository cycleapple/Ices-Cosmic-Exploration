using ECommons.Automation.UIInput;
using FFXIVClientStructs.FFXIV.Client.UI;
using ICE.Compat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_Repair
    {
        private static readonly RepairSweepTracker Sweep = new();
        public static void ResetProgress() => Sweep.Reset();

        public static void Enqueue()
        {
            ResetProgress();
            if (PlayerHelper.AnyNeedsRepair(Char_Info.RepairPercent) && Char_Info.RepairAllGear)
            {
                P.TaskManager.EnqueueMulti
                (
                    new(OpenSelfRepair, "Opening the self repair window"),
                    new(SelfRepair_All, "Executing the self repair", Utils.TaskConfig),
                    new(CloseRepair, "Closing Self Repair"),
                    new(() => SchedulerMain.State = IceState.GrabMission)
                );
            }
            else if (PlayerHelper.NeedsRepair(Char_Info.RepairPercent))
            {
                var currentJob = (uint)Player.Job;

                P.TaskManager.EnqueueMulti
                (
                    new(OpenSelfRepair, "Opening the self repair window"),
                    new(SelfRepair, "Executing the self repair", Utils.TaskConfig),
                    new(CloseRepair, "Closing Self Repair"),
                    new(() =>  SchedulerMain.State = IceState.GrabMission)
                );
            }
        }
        public static unsafe bool? HubCheck()
        {
            string tag = "[Hub Return]";

            if (!C.UseHubReturn)
            {
                IceLogging.Info("We were told we didn't wanna hub return, so we gonna respec this");
                return true;
            }

            if (C.AvoidStellarReturn && !C.AvoidStellarReturnExceptHub)
            {
                IceLogging.Info("Stellar Return is fully disabled, walking to hub instead", tag);
                return true;
            }

            if (CosmicMoonRegistry.TryGetHubCenter(Player.Territory.RowId, out var HubCenter))
            {
                Vector3 PlayerPos = Player.Position;

                if (Player.DistanceTo(HubCenter) < C.HubReturn_Distance)
                {
                    if (PlayerHelper.IsScreenReady())
                    {
                        IceLogging.Info("Player is in the range of the main hub area right now", tag);
                        return true;
                    }
                    else
                    {
                        if (EzThrottler.Throttle("Waiting for screen to be ready", 2000))
                            IceLogging.Verbose("Waiting for screen to be ready", tag);
                    }
                }
                else
                {
                    //Not within the vicinity of the hub area, time to return
                    if (!Player.IsBusy)
                    {
                        if (EzThrottler.Throttle("Returning back to the moon base"))
                            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 26);
                    }
                }

                return false;
            }
            else
            {
                IceLogging.Error($"HEY. WE'RE MISSING THE HUB. THIS ISN'T GOOD. PLEASE ICE FIX THIS <3\n" +
                    $"From: Past Ice.", tag);
                SchedulerMain.State = IceState.Idle;
                P.TaskManager.Tasks.Clear();
                return true;
            }
        }
        public static bool? Repair_PathTo()
        {
            string handle = "[Task_Repair: PathTo]";

            var zoneId = Player.Territory;

            if (NpcData.TryGetNpc(Player.Territory.RowId, NpcData.NpcType.Repair, out var npcEntry))
            {
                Vector3 randomPos = NpcData.GetRandomPointInCircle(npcEntry.Location_Circle, 0.5f);
                if (!Task_NavmeshMove.Task_NavTo(randomPos, distance: 5, npcLoc: npcEntry.Location_Npc).Value)
                {
                    if (EzThrottler.Throttle("Repair move message", 1000))
                        IceLogging.Verbose($"Pathing to repair NPC. Current distance: {Player.DistanceTo(npcEntry.Location_Npc)}", handle);
                }
                else
                {
                    IceLogging.Debug("We're close enough to the repair npc! Continuing on", handle);
                    return true;
                }
            }
            else
            {
                if (EzThrottler.Throttle("Error message: NPC", 5000))
                    IceLogging.Error("Hey! We don't have this npc coded yet, which means I forgot bout it, could you let me know\n" +
                                     $"Planet Territory ID: {Player.Territory.RowId}", handle);
            }

            return false;
        }
        public static unsafe bool? RepairAtNpc()
        {
            var zoneId = Player.Territory;
            IGameObject? gameObject = null;

            if (NpcData.TryGetNpc(Player.Territory.RowId, NpcData.NpcType.Repair, out var npcEntry))
            {
                global::ICE.Compat.TcNpc.TryResolve(npcEntry, out gameObject);   // porting-note(TC): ids drift, positions do not -- see ICE/Compat/TcNpcResolve.cs
            }

            var currentTarget = Svc.Targets.Target;
            var repairAmount = Char_Info.RepairPercent;

            IceLogging.Debug($"{!PlayerHelper.NeedsRepair(99.9f)} | {!(PlayerHelper.AnyNeedsRepair(99.9f) && Char_Info.RepairAllGear)}");

            if (!PlayerHelper.NeedsRepair(99.9f) && !(PlayerHelper.AnyNeedsRepair(99.9f) && Char_Info.RepairAllGear))
            {
                IceLogging.Debug("Repair Complete! Finishing task and closing window");
                return true;
            }
            else if (GenericHelpers.TryGetAddonMaster<Repair>("Repair", out var repair) && repair.IsAddonReady)
            {
                return RepairVisibleWindow(Char_Info.RepairAllGear, true);
            }
            else if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectIconString", out var iconString) && GenericHelpers.IsAddonReady(iconString))
            {
                if (FrameThrottler.Throttle("Firing off repair string"))
                {
                    IceLogging.Debug("Selecting repair from vendor", "[Self Repair Task]");
                    ECommons.Automation.Callback.Fire(iconString, true, 6);
                }
            }
            else
            {
                if (EzThrottler.Throttle("Attempting to target the repair NPC + Interact"))
                {
                    Utils.TargetgameObject(gameObject);
                    Utils.InteractWithObject(gameObject);
                }
            }

            return false;
        }
        public unsafe static bool? OpenSelfRepair()
        {
            if (C.Stop_DarkMatter && PlayerHelper.GetItemCount(Utils.DarkMatter_8Id, out var dmCount) && dmCount < Char_Info.Minimum_DarkMatter)
            {
                IceLogging.ChatInfo("We've ran below the amount of dark matter we want to have, and we can't repair. So we're just hard stopping", "[I.C.E.] Task: Self Repair");
                SchedulerMain.State = IceState.Idle;
                P.TaskManager.Tasks.Clear();
                return true;
            }

            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Repair", out var x) && GenericHelpers.IsAddonReady(x))
            {
                return true;
            }

            if (EzThrottler.Throttle("Opening Self Repair", 1000))
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 6);
            return false;
        }
        public static bool? SelfRepair() => RepairVisibleWindow(false, false);
        public static bool? SelfRepair_All() => RepairVisibleWindow(true, false);

        private static unsafe bool? RepairVisibleWindow(bool all, bool vendor)
        {
            if (Svc.Condition[ConditionFlag.Occupied39]) return false;
            bool needsRepair = all ? PlayerHelper.AnyNeedsRepair(Char_Info.RepairPercent)
                                   : PlayerHelper.NeedsRepair(Char_Info.RepairPercent);
            if (!needsRepair) return true;
            if (Svc.Condition[ConditionFlag.Mounted])
            {
                if (EzThrottler.Throttle("Dismount for repair", 1000))
                    ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
                return false;
            }
            if (!GenericHelpers.TryGetAddonMaster<Repair>("Repair", out var repair) || !repair.IsAddonReady)
                return false;
            if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var yesno) && yesno.IsAddonReady)
            {
                if (EzThrottler.Throttle("Confirm repair", 1000)) yesno.Yes();
                return false;
            }
            var stalled = Sweep.IsStalled(PlayerHelper.RepairConditionFingerprint(), Environment.TickCount64);
            var button = repair.Addon->RepairAllButton;
            if (stalled || button != null && !button->IsEnabled)
            {
                if (all)
                {
                    var node = repair.Addon->AtkUnitBase.GetNodeById(14);
                    var next = node == null ? null : node->GetAsAtkComponentButton();
                    if (next != null && next->IsEnabled && EzThrottler.Throttle("Advance repair category", 1000))
                    {
                        if (Sweep.Advance())
                        {
                            next->ClickAddonButton((AtkComponentBase*)repair.Addon, 2);
                            IceLogging.Debug("Moving to next repair category", "Repair");
                            return false;
                        }
                        return StopFailedRepair(vendor);
                    }
                }
                if (stalled) return StopFailedRepair(vendor);
                return false;
            }
            if (EzThrottler.Throttle("Repair visible category", 1000))
            {
                repair.RepairAll();
                IceLogging.Debug("Repairing current equipment category", "Repair");
            }
            return false;
        }

        private static bool? StopFailedRepair(bool vendor)
        {
            IceLogging.ChatInfo(vendor
                ? "修理後仍有裝備低於門檻，已停止。請確認金幣及修理視窗。"
                : "已檢查修理分類，但仍有裝備低於門檻，已停止。請確認對應製作職業等級及暗物質。", "Repair");
            SchedulerMain.DisablePlugin();
            return true;
        }
        public unsafe static bool? CloseRepair()
        {
            if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var Yesno) && Yesno.IsAddonReady)
            {
                if (FrameThrottler.Throttle("Closing surprise repair window"))
                {
                    ECommons.Automation.Callback.Fire(Yesno.Base, true, -1);
                }
            }

            else if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Repair", out var repairWindow))
            {
                if (GenericHelpers.IsAddonReady(repairWindow))
                {
                    if (EzThrottler.Throttle("Attempting to close out the repair window", 300))
                    {
                        IceLogging.Debug("Closing the repair window", "[Repair Task]");
                        ECommons.Automation.Callback.Fire(repairWindow, true, -1);
                    }
                }
                else
                {
                    return true;
                }
            }
            else return true;

            return false;
        }


    }
}
