using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ICE.Config;
using Lumina.Excel.Sheets;
using Pictomancy;
using System.Collections.Generic;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class Misc_Settings
    {
        public static void Draw()
        {
            OverlaySettings();
            Separator();

            AutoUse();
            Separator();

            TravelSettings();
            Separator();

            RepairSettings();
            Separator();

            TimeRecords();
            Separator();

            MountSelection();
            Separator();

            MissionPlaylists();
            Separator();

            ShowSystemButtons();
            Separator();

            PostMissionCommands();
            Separator();

            ImGuiEx.IconWithText(FontAwesomeIcon.ExclamationTriangle, "安全設定");
            ImGui.Dummy(new Vector2(0, 5));
            SafetySettings.Draw();
        }

        private static void OverlaySettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.WindowMaximize, "資訊浮層");
            ImGui.Dummy(new (0, 5));

            bool showOverlay = C.ShowOverlay;
            if (ImGui.Checkbox("顯示資訊浮層", ref showOverlay))
            {
                C.ShowOverlay = showOverlay;
                C.Save();
            }

            bool ShowSeconds = C.ShowSeconds;
            if (ImGui.Checkbox("顯示秒數", ref ShowSeconds))
            {
                C.ShowSeconds = ShowSeconds;
                C.Save();
            }

            bool showExpOverlay = C.ShowExpBars;
            if (ImGui.Checkbox("在浮層顯示經驗值條", ref showExpOverlay))
            {
                C.ShowExpBars = showExpOverlay;
                C.Save();
            }

            bool showTotalScore = C.ShowTotalScore;
            if (ImGui.Checkbox("顯示總分", ref showTotalScore))
            {
                C.ShowTotalScore = showTotalScore;
                C.Save();
            }

        }

        private static void AutoUse()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.PersonRays, "自動使用");
            ImGui.Dummy(new Vector2(0, 5));

            bool AutoMoonSprint = C.MoonSprint;
            if (ImGui.Checkbox("自動使用月面衝刺", ref AutoMoonSprint))
            {
                C.MoonSprint = AutoMoonSprint;
                C.Save();
            }

            bool DisableLunarAura = C.RemoveStellarStatus;
            if (ImGui.Checkbox("自動移除星極狀態", ref DisableLunarAura))
            {
                C.RemoveStellarStatus = DisableLunarAura;
                C.Save();
            }

            bool DisableRedAlertPathing = C.DisablePathfindingToRedAlert;
            if (ImGui.Checkbox("停用前往紅色警報的尋路", ref DisableRedAlertPathing))
            {
                C.DisablePathfindingToRedAlert = DisableRedAlertPathing;
                C.Save();
            }
        }

        private static void RepairSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Hammer, "修理設定");
            ImGui.Dummy(new Vector2(0, 5));

            bool repairAtVendor = C.RepairAtVendor;
            if (ImGui.Checkbox("在商人處修理", ref repairAtVendor))
            {
                C.RepairAtVendor = repairAtVendor;
                C.Save();
            }

            using (ImRaii.Disabled(repairAtVendor))
            {
                bool selfRepairGather = C.SelfRepairGather;
                if (ImGui.Checkbox("採集職自行修理", ref selfRepairGather))
                {
                    C.SelfRepairGather = selfRepairGather;
                    C.Save();
                }

                bool selfRepairCrafter = C.SelfRepairCrafter;
                if (ImGui.Checkbox("製作職自行修理", ref selfRepairCrafter))
                {
                    C.SelfRepairCrafter= selfRepairCrafter;
                    C.Save();
                }
            }

            float repairAmount = C.RepairPercent;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("###Repair %", ref repairAmount, 0f, 99f, "%.0f%%"))
            {
                if (C.RepairPercent != repairAmount)
                {
                    C.RepairPercent = (int)repairAmount;
                    C.SaveDebounced();
                }
            }
        }

        private static void TimeRecords()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Clock, "紀錄設定");
            ImGui.Dummy(new Vector2(0, 5));

            int TimeHistory = C.TimeHistoryLimit;
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("保留的平均耗時紀錄數", ref TimeHistory))
            {
                C.TimeHistoryLimit = TimeHistory;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("設為 0 以下可保留全部紀錄\n" +
                                 "設為 0 以上則依指定數量保留");
            }
        }

        private static bool visualizeRadius = false;
        private static bool visualizeDismountRadius = false;
        private static Dictionary<uint, string> availableMounts = new();

        private static string mountSearchText = "";
        private static int mountDisplayOffset = 0;
        private static int mountItemsPerPage = 10;

        private static unsafe void MountSelection()
        {
            bool mountOutsideMission = C.UseMountOutsideMission;
            bool mountInMission = C.UseMountInMission;
            float minMountRange = C.MountRadius;
            float dismountRange = C.DismountRadius;

            ImGuiEx.IconWithText(FontAwesomeIcon.Feather, "坐騎設定");
            ImGui.Dummy(new Vector2(0, 5));

            if (ImGui.Button("選擇坐騎"))
            {
                availableMounts.Clear();
                availableMounts[0] = "Mount Roulette";

                var mountSheet = Svc.Data.GetExcelSheet<Mount>();

                foreach (var mountItem in mountSheet)
                {
                    //Checking to see if the current mount is unlocked
                    if (!PlayerState.Instance()->IsMountUnlocked(mountItem.RowId)) continue;

                    string mountName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mountItem.Singular.ToString().ToLower());
                    uint id = mountItem.RowId;

                    availableMounts[id] = mountName;
                }

                mountSearchText = "";
                mountDisplayOffset = 0;

                ImGui.OpenPopup("Mount Options");
            }
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"坐騎：{C.MountName}");

            if (ImGui.BeginPopup("Mount Options"))
            {
                // Search box
                ImGui.InputText("搜尋", ref mountSearchText, 100);

                // Filter mounts based on search
                var filteredMounts = availableMounts
                    .Where(kvp => string.IsNullOrEmpty(mountSearchText) ||
                                  kvp.Value.Contains(mountSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Calculate page count here, just to peeps know how many pages there are
                int totalItems = filteredMounts.Count;
                int maxOffset = Math.Max(0, totalItems - mountItemsPerPage);
                mountDisplayOffset = Math.Min(mountDisplayOffset, maxOffset);

                // Display current page of mounts
                var displayMounts = filteredMounts
                    .Skip(mountDisplayOffset)
                    .Take(mountItemsPerPage);

                foreach (var mount in displayMounts)
                {
                    if (ImGui.Selectable($"{mount.Value}##{mount.Key}"))
                    {
                        C.MountId = mount.Key;
                        C.MountName = mount.Value;
                        C.Save();
                        ImGui.CloseCurrentPopup();
                    }
                }

                // Navigation buttons
                ImGui.Separator();

                if (ImGui.Button("上一頁") && mountDisplayOffset > 0)
                {
                    mountDisplayOffset = Math.Max(0, mountDisplayOffset - mountItemsPerPage);
                }

                ImGui.SameLine();
                ImGui.Text($"{mountDisplayOffset + 1}-{Math.Min(mountDisplayOffset + mountItemsPerPage, totalItems)}／{totalItems}");

                ImGui.SameLine();
                if (ImGui.Button("下一頁") && mountDisplayOffset < maxOffset)
                {
                    mountDisplayOffset = Math.Min(maxOffset, mountDisplayOffset + mountItemsPerPage);
                }

                ImGui.EndPopup();
            }

            if (ImGui.Checkbox("任務外使用坐騎", ref mountOutsideMission))
            {
                C.UseMountOutsideMission = mountOutsideMission;
                C.Save();
            }

            if (ImGui.Checkbox("任務中使用坐騎", ref mountInMission))
            {
                C.UseMountInMission = mountInMission;
                C.Save();
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("最小騎乘距離", ref minMountRange, 1))
            {
                C.MountRadius = minMountRange;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Checkbox("顯示範圍", ref visualizeRadius);
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("下坐騎目標距離", ref dismountRange, 1))
            {
                C.DismountRadius = dismountRange;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Checkbox("顯示下坐騎範圍", ref visualizeDismountRadius);

            using (var drawList = PictoService.Draw())
            {
                if (drawList == null)
                    return;

                var playerPos = Player.Position;

                if (visualizeRadius)
                    PictoService.VfxRenderer.AddCircle("Mount_Radius Circle", playerPos, C.MountRadius, Utils.FromUintABGR(2616716297));
                if (visualizeDismountRadius)
                    PictoService.VfxRenderer.AddCircle("Dismount_Radius Circle", playerPos, C.DismountRadius, Utils.FromUintABGR(2601121571));
            }
        }

        private static void ShowSystemButtons()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.WindowRestore, "顯示／隱藏分頁");
            ImGui.Dummy(new(0, 5));

            bool showStopWhen = C.Show_StopWhen;
            if (ImGui.Checkbox("顯示「停止條件」分頁", ref showStopWhen))
            {
                C.Show_StopWhen = showStopWhen;
                C.Save();
            }

            bool showGProfile = C.Show_GatheringProfile;
            if (ImGui.Checkbox("顯示「採集設定檔」分頁", ref showGProfile))
            {
                C.Show_GatheringProfile = showGProfile;
                C.Save();
            }

            bool showMissionPrio = C.Show_MissionPriority;
            if (ImGui.Checkbox("顯示「任務優先順序」分頁", ref showMissionPrio))
            {
                C.Show_MissionPriority = showMissionPrio;
                C.Save();
            }

            bool showMisc = C.Show_MiscSettings;
            if (ImGui.Checkbox("顯示「其他設定」分頁", ref showMisc))
            {
                C.Show_MiscSettings = showMisc;
                C.Save();
            }

            bool showHubActivities = C.Show_HubActivities;
            if (ImGui.Checkbox("顯示「據點活動」區段", ref showHubActivities))
            {
                C.Show_HubActivities = showHubActivities;
                C.Save();
            }
        }

        private static string playlistName = string.Empty;

        private static void MissionPlaylists()
        {
            C.MissionPlaylists ??= new();

            ImGuiEx.IconWithText(FontAwesomeIcon.List, "任務預設");
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.TextWrapped("儲存目前啟用的任務組合。載入預設會關閉未列入的任務，但不會改變任務優先順序或個別任務設定。");

            ImGui.SetNextItemWidth(220);
            ImGui.InputText("預設名稱", ref playlistName, 100);
            ImGui.SameLine();

            var normalizedName = playlistName.Trim();
            using (ImRaii.Disabled(normalizedName.Length == 0))
            {
                if (ImGui.Button("儲存目前啟用項目"))
                {
                    C.MissionPlaylists[normalizedName] = C.MissionConfig
                        .Where(entry => entry.Value.Enabled)
                        .Select(entry => entry.Key)
                        .Distinct()
                        .Order()
                        .ToList();
                    playlistName = string.Empty;
                    C.Save();
                }
            }

            if (C.MissionPlaylists.Count == 0)
            {
                ImGui.TextDisabled("尚未建立任務預設。");
                return;
            }

            string? deletePlaylist = null;
            foreach (var (name, missionIds) in C.MissionPlaylists)
            {
                var savedMissionIds = missionIds ?? [];
                ImGui.PushID(name);
                var knownMissionCount = savedMissionIds.Distinct().Count(C.MissionConfig.ContainsKey);
                ImGui.Text($"{name}（{knownMissionCount} 個任務）");
                ImGui.SameLine();
                if (ImGui.Button("載入"))
                {
                    var selected = savedMissionIds.ToHashSet();
                    foreach (var (missionId, settings) in C.MissionConfig)
                        settings.Enabled = selected.Contains(missionId);
                    C.Save();
                }
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, "刪除"))
                    deletePlaylist = name;
                ImGui.PopID();
            }

            if (deletePlaylist != null)
            {
                C.MissionPlaylists.Remove(deletePlaylist);
                C.Save();
            }
        }

        private static void PostMissionCommands()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Play, "任務完成後指令");
            ImGui.Dummy(new Vector2(0, 5));

            ImGui.TextWrapped("在下方輸入每次任務完成後要執行的指令清單。\n" +
                              "可用來依序執行本插件未內建的其他操作。\n" +
                              "若需要更複雜的流程，建議建立 SND 腳本，再由此處於任務完成後執行。");

            if (ImGui.Button("新增指令"))
            {
                C.PostMissionCommands.Add(new Config.MissionCommand 
                { 
                    command = "", 
                    Delay = 0,
                });
                C.Save();
            }

            MissionCommand? toRemove = null;
            int entryCounter = 0;

            if (ImGui.BeginTable("Mission Commands", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("指令");
                ImGui.TableSetupColumn("延遲");
                ImGui.TableSetupColumn("移除");

                ImGui.TableHeadersRow();

                foreach (var entry in C.PostMissionCommands)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.SetNextItemWidth(200);

                    ImGui.PushID($"{entryCounter}_MissionCommand");
                    string command = entry.command;
                    if (ImGui.InputText("##Command", ref command))
                    {
                        entry.command = command;
                        C.SaveDebounced();
                    }

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(100);
                    int delay = entry.Delay;
                    if (ImGui.InputInt("###Delay", ref delay))
                    {
                        entry.Delay = delay;
                        C.SaveDebounced();
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, $"remove{C.PostMissionCommands.IndexOf(entry)}"))
                    {
                        toRemove = entry;
                    }
                    ImGui.PopID();
                    entryCounter += 1;
                }

                if (toRemove != null)
                {
                    C.PostMissionCommands.Remove(toRemove);
                    C.Save();
                }

                ImGui.EndTable();
            }
        }

        private static void TravelSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Route, "宇宙探索移動");
            ImGui.Dummy(new Vector2(0, 5));

            bool useHubReturn = C.UseHubReturn;
            if (ImGui.Checkbox("使用返回據點縮短路程", ref useHubReturn))
            {
                C.UseHubReturn = useHubReturn;
                C.Save();
            }

            bool useRedAlertNpc = C.UseRedAlertNpc;
            if (ImGui.Checkbox("使用緊急任務傳送人員", ref useRedAlertNpc))
            {
                C.UseRedAlertNpc = useRedAlertNpc;
                C.Save();
            }
        }

        private static void Separator()
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
        }
    }
}
