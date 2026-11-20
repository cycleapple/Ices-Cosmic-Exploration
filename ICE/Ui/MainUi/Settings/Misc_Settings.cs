using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ICE.Ui.MainUi.Settings.Settings_Table;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.ImGuiTools;
using System.Collections.Generic;
using static ICE.ConfigFiles.Config;

namespace ICE.Ui.MainUi.Settings
{
    internal class Misc_Settings
    {
        public static void Draw()
        {
            OverlaySettings();
            Separator();
            AutoUse();
            Separator();
            GoldMissionRemover();
            Separator();
            SafetySettings.Draw();
            Separator();
            Separator();
            TimeRecords();
            Separator();
            PostMissionCommands();
            Separator();
            FunSettings();
#if DEBUG
            Separator();
            DebugTab.Draw();
#endif
        }

        public static void OverlaySettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.WindowMaximize, "資訊浮層");
            ImGui.Dummy(new (0, 5));

            bool showOverlay = C.ShowOverlay;
            if (ImGui.Checkbox("自動開啟浮動視窗", ref showOverlay))
            {
                C.ShowOverlay = showOverlay;
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("開啟浮動視窗"))
            {
                if (!P.overlayWindow.IsOpen)
                    P.overlayWindow.IsOpen = true;
            }
            ImGui.SameLine();
            bool useCogsIcon = C.Overlay_UseCogsIcon;
            if (ImGui.Checkbox("使用齒輪按鈕圖示", ref useCogsIcon))
            {
                C.Overlay_UseCogsIcon = useCogsIcon;
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
            if (showExpOverlay)
            {
                ImGui.SameLine();
                bool hideWhenMaxed = C.ShowExpBars_HideWhenMaxed;
                if (ImGui.Checkbox("僅在尚未達上限時顯示", ref hideWhenMaxed))
                {
                    C.ShowExpBars_HideWhenMaxed = hideWhenMaxed;
                    C.Save();
                }
            }

            bool showClassScore = C.ShowCurrentScore;
            if (ImGui.Checkbox("顯示目前職業分數", ref showClassScore))
            {
                C.ShowCurrentScore = showClassScore;
                C.Save();
            }
            ImGui.SameLine();
            bool showTotalScore = C.ShowTotalScore;
            if (ImGui.Checkbox("顯示總分", ref showTotalScore))
            {
                C.ShowTotalScore = showTotalScore;
                C.Save();
            }
            ImGui.SameLine();
            bool showMasteryScore = C.ShowMasteryScore;
            if (ImGui.Checkbox("顯示工具精通分數", ref showMasteryScore))
            {
                C.ShowMasteryScore = showMasteryScore;
                C.Save();
            }

            bool AutoResize = C.Overlay_AutoResize;
            if (ImGui.Checkbox("自動調整浮動視窗大小", ref AutoResize))
            {
                C.Overlay_AutoResize = AutoResize;
                C.Save();
            }


            bool highlightTokenWeather = C.Overlay_HighlightTokenWeather;
            if (ImGui.Checkbox("標示 EX+ 兌換道具對應天氣", ref highlightTokenWeather))
            {
                C.Overlay_HighlightTokenWeather = highlightTokenWeather;
                C.Save();
            }

            bool showSelectedWeatherMissions = C.Overlay_WeatherSelected;
            if (ImGui.Checkbox($"滑鼠停在天氣上時顯示已啟用任務", ref showSelectedWeatherMissions))
            {
                C.Overlay_WeatherSelected = showSelectedWeatherMissions;
                C.Save();
            }

            bool filterByCurrentJob = C.Overlay_FilterByCurrentJob;
            if (ImGui.Checkbox("僅顯示目前職業", ref filterByCurrentJob))
            {
                C.Overlay_FilterByCurrentJob = filterByCurrentJob;
                C.Save();
            }
            if (!filterByCurrentJob)
            {
                float scale = ImGuiHelpers.GlobalScale;
                float iconSize = 26 * scale;
                float iconSpacing = 4;
                var classDict = new Dictionary<uint, string>
                {
                    [8] = "CRP", [9] = "BSM", [10] = "ARM", [11] = "GSM",
                    [12] = "LTW", [13] = "WVR", [14] = "ALC", [15] = "CUL",
                    [16] = "MIN", [17] = "BTN", [18] = "FSH",
                };
                foreach (var (jobId, name) in classDict)
                {
                    bool isSelected = C.Overlay_FilterJobs.Contains(jobId);
                    var icon = isSelected
                        ? CosmicHelper.ClassInfoDict.TryGetValue(jobId, out var tex) ? tex.JobIcon.GetWrapOrEmpty() : null
                        : ImGui_Ice.GetGreyscaleJob(jobId);
                    if (icon != null && ImGui_Ice.DrawStyledImageButton(icon, new Vector2(iconSize, iconSize), isSelected))
                    {
                        if (isSelected)
                            C.Overlay_FilterJobs.Remove(jobId);
                        else
                            C.Overlay_FilterJobs.Add(jobId);
                        C.Save();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(name);
                    ImGui.SameLine(0, iconSpacing);
                }
                ImGui.NewLine();
            }

            bool disableHudClipping = C.DisableHudClipping;
            if (ImGui.Checkbox("允許浮動視窗覆蓋遊戲介面", ref disableHudClipping))
            {
                C.DisableHudClipping = disableHudClipping;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("啟用後，浮動視窗可顯示在遊戲原生介面上方。");
            }

        }
        private static void AutoUse()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.PersonRays, "自動使用");
            ImGui.Dummy(new Vector2(0, 5));

            bool DisableLunarAura = C.RemoveStellarStatus;
            if (ImGui.Checkbox("自動移除星極狀態", ref DisableLunarAura))
            {
                C.RemoveStellarStatus = DisableLunarAura;
                C.Save();
            }
            ImGui.SameLine();
            ImGuiEx.IconWithTooltip(FontAwesomeIcon.InfoCircle,
                                   "Automatically removes the Star Contributor visual effect (the glow you get for being a top contributor).\n" +
                                   "The buff restores itself when you re-enter the zone.");

            bool autoStartOnMoonEnter = C.StartUponEnterMoon;
            if (ImGui.Checkbox("進入宇宙探索區域後自動開始", ref autoStartOnMoonEnter))
            {
                C.StartUponEnterMoon = autoStartOnMoonEnter;
                C.Save();
            }
            ImGui.SameLine();
            ImGuiEx.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                                   "This will check to see if you're on a gathering/crafting class upon first entering the moon.\n" +
                                   "If you are, it will automatically start as if you had pressed the start button yourself\n" +
                                   "Really useful if you have a tool to auto-log you in/if you just want to enter the moon and go\n" +
                                   "This will ONLY run upon first entry.");
            ImGui.Dummy(Vector2.Zero);
        }
        private static void GoldMissionRemover()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Medal, "任務完成後設定");

            bool removeGold = C.RemoveAfterGold;
            if (ImGui.Checkbox("取得金牌後移除任務", ref removeGold))
            {
                C.RemoveAfterGold = removeGold;
                C.Save();
            }

            using (ImRaii.Disabled(!removeGold))
            {
                bool keepARanks = C.KeepARanks;
                if (ImGui.Checkbox("Keep \"A Rank\" missions and below", ref keepARanks))
                {
                    C.KeepARanks = keepARanks;
                    C.Save();
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
        private static void PostMissionCommands()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Play, "任務完成後指令");
            ImGui.Dummy(new Vector2(0, 5));

            ImGui.TextWrapped("在下方輸入每次任務完成後要執行的指令清單。\n" +
                              "可用來依序執行本插件未內建的其他操作。\n" +
                              "若需要更複雜的流程，建議建立 SND 腳本，再由此處於任務完成後執行。");

            if (ImGui.Button("新增指令"))
            {
                C.PostMissionCommands.Add(new MissionCommand
                {
                    command = "",
                    Delay = 0,
                });
                C.Save();
            }

            MissionCommand? toRemove = null;
            int entryCounter = 0;

            if (ImGui.BeginTable("任務完成後指令", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
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
        private static void FunSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Heart, "其他顯示選項");
            var crazyEnabled = C.CrazyTaxiArrow;
            if (ImGui.Checkbox("導航時顯示方向箭頭", ref crazyEnabled))
            {
                C.CrazyTaxiArrow = crazyEnabled;
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
