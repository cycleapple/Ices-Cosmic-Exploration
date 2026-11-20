using Dalamud.Interface;
using ECommons.GameHelpers;
using ICE.Ui.Debug_Tabs.Debug_Ui;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using ICE.Utilities.ImGuiTools;
using static ICE.ConfigFiles.Config;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class TravelSettings
    {
        private static FishingDebug _fishingDebug = null;

        public static unsafe void Draw()
        {
            if (_fishingDebug == null)
            {
                _fishingDebug = new FishingDebug();
            }

            PathfindingSettings();

            Separator();
            StuckSettings();

            Separator();
            CraftingLocations();

            Separator();
            FishingLocations();
        }

        private static void Separator()
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
        }

        private static void PathfindingSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Route, "尋路");
            ImGui.Dummy(new Vector2(0, 5));

            bool stellarSprint = C.MoonSprint;
            if (ImGui.Checkbox("自動使用宇宙衝刺", ref stellarSprint))
            {
                C.MoonSprint = stellarSprint;
                C.Save();
            }

            bool closestNode = C.ClosestNodeSelection;
            if (ImGui.Checkbox("優先前往最近的可採集點", ref closestNode))
            {
                C.ClosestNodeSelection = closestNode;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Always navigate to the closest targetable node instead of following the fixed route order.\nUseful for timed EX+ missions where speed matters.");
            }

            bool randomize = C.RandomizeWaypoints;
            if (ImGui.Checkbox("隨機偏移移動目標位置", ref randomize))
            {
                C.RandomizeWaypoints = randomize;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Adds a small random offset to navigation destinations so the character doesn't always follow the exact same path");
            }
            if (randomize)
            {
                ImGui.SameLine();
                float radius = C.RandomizeWaypointsRadius;
                ImGui.SetNextItemWidth(100);
                if (ImGui.SliderFloat("隨機偏移半徑（星碼）", ref radius, 0.5f, 1.0f, "%.1f"))
                {
                    C.RandomizeWaypointsRadius = radius;
                    C.SaveDebounced();
                }
                bool showDebug = C.RandomizeWaypointsDebug;
                if (ImGui.Checkbox("顯示偏移位置的除錯標記", ref showDebug))
                {
                    C.RandomizeWaypointsDebug = showDebug;
                    C.Save();
                }
            }

            int GatherFanRandom = C.GatherFanSectionSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("採集接近角度", ref GatherFanRandom, 0, 360))
            {
                C.GatherFanSectionSize = GatherFanRandom;
                C.SaveDebounced();
            }
            ImGui.SameLine();
            ImGui_Ice.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "This will adjust how much of the center point of the fan it will randomize from.\n" +
                "360 = the whole fan will be available for selection\n" +
                "Anything besides that will chose within that fan (if it's available)", false);

            bool selfGather = C.Gather_NoNav;
            if (ImGui.Checkbox("停用採集點之間的自動尋路", ref selfGather))
            {
                C.Gather_NoNav = selfGather;
                C.SaveDebounced();
            }
            ImGui.SameLine();
            ImGui_Ice.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "This will disable the pathfinding between the nodes WHILE in the mission\n" +
                "But still allow the automation of skills/gathering actions/desynth between missions\n" +
                "This is VERY testing beta, so there might be issues\n" +
                "I swear on cuthulu's name if you enable this then ask \"Why it don't work\"" +
                "You'll be banned by the shadow realm");


            bool useHubReturn = C.UseHubReturn;
            if (ImGui.Checkbox("使用據點返回", ref useHubReturn))
            {
                C.UseHubReturn = useHubReturn;
                C.Save();
            }
            ImGui.SameLine();
            bool useAethernet = C.UseAethernet;
            if (ImGui.Checkbox("使用以太傳送網", ref useAethernet))
            {
                C.UseAethernet = useAethernet;
                C.Save();
            }

            bool useRedAlertNpc = C.UseRedAlertNpc;
            if (ImGui.Checkbox("使用緊急任務 NPC 傳送", ref useRedAlertNpc))
            {
                C.UseRedAlertNpc = useRedAlertNpc;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("測試功能，可能尚未正常運作");

            bool avoidStellarReturn = C.AvoidStellarReturn;
            if (ImGui.Checkbox("尋路時避免使用宇宙返回", ref avoidStellarReturn))
            {
                C.AvoidStellarReturn = avoidStellarReturn;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("When enabled, the pathfinder will not use Stellar Return to travel to gathering nodes.\nThis applies to both Hub Return and Hub + Aethernet travel methods.");
            }
            if (C.AvoidStellarReturn)
            {
                ImGui.SameLine();
                bool exceptHub = C.AvoidStellarReturnExceptHub;
                if (ImGui.Checkbox("據點活動除外", ref exceptHub))
                {
                    C.AvoidStellarReturnExceptHub = exceptHub;
                    C.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled, Stellar Return will still be used to return to the hub\nfor activities like credit purchases, gambling, drone bits, and repairs.");
                }
            }

            var minHubReturnDistance = C.HubReturn_Distance;
            ImGui.SetNextItemWidth(200);
            if (ImGui.DragFloat("使用據點返回的最低距離（星碼）", ref minHubReturnDistance))
            {
                C.HubReturn_Distance = minHubReturnDistance;
                C.SaveDebounced();
            }

            bool DisableRedAlertPathing = C.DisablePathfindingToRedAlert;
            if (ImGui.Checkbox("停用前往紅色警報的尋路", ref DisableRedAlertPathing))
            {
                C.DisablePathfindingToRedAlert = DisableRedAlertPathing;
                C.Save();
            }

            bool DisableHubActivies_RE = C.DisableHub_Critical;
            if (ImGui.Checkbox("緊急任務期間暫停據點活動", ref DisableHubActivies_RE))
            {
                C.DisableHub_Critical = DisableHubActivies_RE;
                C.Save();
            }

            bool delayAether = C.Delay_Aethernet;
            if (ImGui.Checkbox("以太傳送網與 NPC 傳送前加入延遲", ref delayAether))
            {
                C.Delay_Aethernet = delayAether;
                C.Save();
            }
            ImGuiEx.HelpMarker("Adds a random delay before interacting with the aethershard / red alert npc travel.\n" +
                "The delays will be before, and a little bit inbetween interacting with menus");
        }
        private static void StuckSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.ExclamationTriangle, "卡住偵測");
            ImGui.Dummy(new Vector2(0, 5));

            bool unstuckEnabled = C.JumpIfStuck_V2 || C.RetargetIfStuck;
            if (ImGui.Checkbox("導航移動卡住時：", ref unstuckEnabled))
            {
                if (unstuckEnabled)
                    C.JumpIfStuck_V2 = true;
                else
                {
                    C.JumpIfStuck_V2 = false;
                    C.RetargetIfStuck = false;
                }
                C.Save();
            }
            ImGui.SameLine();
            ImGuiEx.HelpMarker(
                "When stuck during navmesh movement for the configured delay:\n" +
                "- Jump: attempts to jump over the obstacle\n" +
                "- Retarget: stops and re-pathfinds to the destination (re-randomizes if enabled)");
            if (!unstuckEnabled) ImGui.BeginDisabled();
            if (ImGui.RadioButton("跳躍", C.JumpIfStuck_V2 && !C.RetargetIfStuck))
            {
                C.JumpIfStuck_V2 = true;
                C.RetargetIfStuck = false;
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("重新選取目標", C.RetargetIfStuck))
            {
                C.RetargetIfStuck = true;
                C.JumpIfStuck_V2 = false;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Text("等待");
            ImGui.SameLine();
            int stuckDelay = C.StuckDelayMs;
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("毫秒後判定卡住###StuckDelay", ref stuckDelay, 500, 3000))
            {
                if (C.StuckDelayMs != stuckDelay)
                {
                    C.StuckDelayMs = stuckDelay;
                    C.SaveDebounced();
                }
            }
            if (!unstuckEnabled) ImGui.EndDisabled();
        }
        private static void CraftingLocations()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.MapPin, "製作返回位置");
            ImGui.Dummy(new Vector2(0, 5));

            bool usePersonalLocations = C.PersonalReturnSpot;
            if (ImGui.Checkbox("使用自訂返回位置", ref usePersonalLocations))
            {
                C.PersonalReturnSpot = usePersonalLocations;
                C.Save();
            }
            if (usePersonalLocations)
            {
                var territory = Player.Territory.RowId;
                var location = Player.Position;
                ImGui.SameLine();
                if (C.CrafterLocations.TryGetValue(territory, out var moonLoc))
                {
                    if (ImGui.Button("設為目前位置"))
                    {
                        C.CrafterLocations[territory] = location;
                        C.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text($"({moonLoc.X:N1}, {moonLoc.Y:N1}, {moonLoc.Z:N1})");
                }
                else
                {
                    if (ImGui.Button("新增位置"))
                    {
                        C.CrafterLocations[territory] = Player.Position;
                        C.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("尚未設定位置");
                }
            }
        }
        private static void FishingLocations()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Fish, "自訂釣魚位置");
            ImGui.SameLine();
            ImGui_Ice.IconWithTooltip(FontAwesomeIcon.QuestionCircle, "A way for you to save your own positions if you choose to not use a randomized spot that's included in the plugin\n" +
                "You don't have to use this, it will just use a random spot if:\n" +
                "1: A position is saved:\n" +
                "2: A random spot even is saved", false);
            ImGui.Dummy(new Vector2(0, 5));

            var currentTerritory = Player.Territory.RowId;

            if (GatheringUtil.MoonFishingLocations.TryGetValue(currentTerritory, out var fishingHoles))
            {
                ImGui.Text($"Planet: {Player.Territory.Value.PlaceName.Value.Name}");
                ImGui.Checkbox("顯示釣點方向判定", ref _fishingDebug.ShowFishRay);
                if (Player.Object is { } player && _fishingDebug.ShowFishRay)
                {
                    _fishingDebug.Draw();
                }

                ImGui.Separator();

                foreach (var hole in fishingHoles.Keys)
                {
                    // Find existing entry for this zone + map coord, or creating a new one if one doesn't exist
                    var entry = C.Personal_FishLocation.FirstOrDefault(f => f.ZoneId == currentTerritory && f.MapCoords == hole);

                    if (entry == null)
                    {
                        entry = new FishingLocations
                        {
                            ZoneId = currentTerritory,
                            X = hole.X,
                            Y = hole.Y,
                            WorldPosition = null
                        };
                        C.Personal_FishLocation.Add(entry);
                        C.SaveDebounced();
                    }

                    ImGui.PushID($"{hole}_Flag");

                    if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Flag, $"  X: {hole.X:N2} Y: {hole.Y:N2}"))
                    {
                        var mission = CosmicHelper.SheetMissionDict.Where(x => x.Value.MapPosition == hole).FirstOrDefault();
                        Utils.SetGatheringRing(mission.Value.TerritoryId, (int)hole.X, (int)hole.Y, mission.Value.Radius, $"{hole.X:N2} {hole.Y:N2}");
                    }
                    ImGui.SameLine();

                    string currentPos = entry.WorldPosition == null ? "Add New" : $"移除";

                    if (ImGui.Button($"{currentPos}"))
                    {
                        entry.WorldPosition = entry.WorldPosition == null ? Player.Position : null;
                        C.Save();
                    }

                    if (entry.WorldPosition != null)
                    {
                        ImGui.SameLine();
                        ImGui.Text($"{entry.WorldPosition.Value:N2}");
                    }

                    ImGui.PopID();
                }
            }
            else
            {
                ImGui.Text($"Current planet has no stored fishing holes in the sheets. (Might need to be added?)");
            }
        }
    }
}
