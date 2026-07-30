using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ICE.Ui.MainUi.Settings.Settings_Table;
using ICE.Utilities.ImGuiTools;
using System.Collections.Generic;
using System.Reflection;

namespace ICE.Ui.MainUi.ModeSelect
{
    internal class modeSelect_Standard
    {
        public static void Draw()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 10).Push(ImGuiStyleVar.ChildBorderSize, 1);

            // Header at the top
            float scale = ImGuiHelpers.GlobalScale;

            bool autoSelectMoon = C.AutoSelectMoon;
            if (autoSelectMoon)
            {
                if (PlayerHelper.IsInSinusArdorum() && (!C.ShowSinusMissions || C.ShowPhaennaMissions))
                {
                    C.ShowSinusMissions = true;
                    C.ShowPhaennaMissions = false;
                    C.Save();
                }
                else if (PlayerHelper.IsInPhaenna() && (C.ShowSinusMissions || !C.ShowPhaennaMissions))
                {
                    C.ShowSinusMissions = false;
                    C.ShowPhaennaMissions = true;
                    C.Save();
                }
            }

            using (var headerChild = ImRaii.Child("##modeSelect_StandardHeader", new Vector2(0, 45 * scale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!headerChild.Success) return;

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10 * scale);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5 * scale);

                string modeType = string.Empty;
                FontAwesomeIcon modeIcon = FontAwesomeIcon.List;

                bool relicMode = C.XPRelicGrind;
                bool provisionalMode = C.GrindProvisionals;
                bool standard = (!relicMode && !provisionalMode);

                if (standard)
                    modeType = "標準";
                else if (relicMode)
                {
                    modeType = "宇宙工具培育";
                    modeIcon = FontAwesomeIcon.ArrowUpRightDots;
                }
                else if (provisionalMode)
                {
                    modeType = "臨時任務";
                    modeIcon = FontAwesomeIcon.Cloud;
                }

                ImGuiEx.IconWithText(modeIcon, $"{modeType}模式");

                ImGui.SameLine(0, 10 * scale);

                // Adjust the Y position to center the button vertically with the text
                float textHeight = ImGui.GetTextLineHeight();
                float buttonHeight = ImGui.GetFrameHeight();
                float yOffset = (textHeight - buttonHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "選擇模式"))
                {
                    ImGui.OpenPopup("Mode Select | Select Mode Window");
                }
                if (ImGui.BeginPopup("Mode Select | Select Mode Window"))
                {
                    ImGui.Text("選擇模式");
                    ImGui.Separator();

                    if (ImGui.RadioButton("標準", standard))
                    {
                        C.XPRelicGrind = false;
                        C.GrindProvisionals = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("標準模式\n" +
                                       "→ 選擇要重複執行的任務，優先順序如下：\n" +
                                       "→ 關鍵任務 → 臨時任務（連續／限時／天候）→ 標準任務（A→D）\n" +
                                       "→ 選好任務後即可開始執行。");
                    if (ImGui.RadioButton("宇宙工具培育", relicMode))
                    {
                        C.XPRelicGrind = true;
                        C.GrindProvisionals = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("宇宙工具培育\n" +
                                       "→ 自動選擇最適合提升宇宙工具的任務。\n" +
                                       "→ 依照工具進入下一階段所需的條件計算權重。\n" +
                                       "→ 若只想執行特定任務，可啟用對應選項並自行勾選。");
                    if (ImGui.RadioButton("臨時任務周回", provisionalMode))
                    {
                        C.XPRelicGrind = false;
                        C.GrindProvisionals = true;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("臨時任務周回\n" +
                                       "→ 重複執行已啟用的臨時任務（天候／限時／連續）。\n" +
                                       "→ 可用於多個職業，並設定職業與任務類型的優先順序。\n" +
                                       "→ 適合累積各職業分數與代幣，或在特定時間執行指定任務。");

                    ImGui.EndPopup();
                }

                uint currentJobId = Player.JobId;
                bool usingSupportedJob = CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId);

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
                {
                    if (ImGui.Button("開始", new Vector2(150 * scale, 0)))
                    {
                        SchedulerMain.EnablePlugin();
                    }
                }

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
                using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f)))
                    {
                        if (ImGui.Button("停止", new Vector2(150 * scale, 0)))
                        {
                            SchedulerMain.DisablePlugin();
                        }
                    }
                }
            }

            if (ImGui.BeginTable("modeSelect_TableHeader", 4, ImGuiTableFlags.SizingFixedFit, Vector2.Zero))
            {
                ImGui.TableSetupColumn("職業選擇");
                ImGui.TableSetupColumn("其他設定");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                bool tableSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("表格設定", FontAwesomeIcon.Table);

                ImGui.TableNextColumn();
                bool missionSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("任務設定", FontAwesomeIcon.UserCog);

                bool relicGrindExpanded = false;
                if (C.XPRelicGrind)
                {
                    ImGui.TableNextColumn();
                    relicGrindExpanded = modeSelect_Tools.DrawCompactCategoryHeader("宇宙工具培育設定", FontAwesomeIcon.ArrowUpRightDots);
                }

                bool completionExpanded = false;
                if (C.ShowCompletionWindow)
                {
                    ImGui.TableNextColumn();
                    completionExpanded = modeSelect_Tools.DrawCompactCategoryHeader("完成進度表設定", FontAwesomeIcon.Trophy);
                }

                bool showNextColumn = tableSettingExpanded || missionSettingExpanded || (relicGrindExpanded && C.XPRelicGrind) || (completionExpanded && C.ShowCompletionWindow);

                if (showNextColumn)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (tableSettingExpanded)
                    {
                        Settings_TableColumns.ColumnSettings();
                    }

                    ImGui.TableNextColumn();
                    if (missionSettingExpanded)
                    {
                        Settings_TableColumns.GeneralMissionSettings();
                    }

                    if (C.XPRelicGrind && relicGrindExpanded)
                    {
                        ImGui.TableNextColumn();

                        bool relicTurnin = C.TurninRelic;
                        if (ImGui.Checkbox($"宇宙工具完成時交付##RelicTurnin_RelicGrind", ref relicTurnin))
                        {
                            if (relicTurnin)
                                C.GrindProvisionals = false;

                            C.TurninRelic = relicTurnin;
                            C.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("以下說明此功能的運作方式；若未來行為變更，此提示也會同步更新。\n" +
                                             "1：會檢查角色目前實際使用的職業，而非選單中選取的職業，以判斷宇宙工具繳交。\n" +
                                             "2：若要全自動執行，不能裝備該宇宙工具。\n" +
                                             "3：此功能的優先度高於「宇宙工具繳交時停止」。兩者同時啟用時會繳交並繼續，而非停止。\n" +
                                             "4：若目前為製作職業，繳交後可返回原先進行製作的位置；此行為可自行停用。");
                        }

                        ImGui.Separator();

                        bool EnableRelicXp = C.XPRelicGrind;
                        if (ImGui.Checkbox("自動選擇宇宙工具經驗值任務", ref EnableRelicXp))
                        {
                            if (EnableRelicXp)
                            {
                                C.GrindProvisionals = false;
                            }
                            C.XPRelicGrind = EnableRelicXp;
                            C.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("請注意：這只會在基本任務分頁培育宇宙工具經驗值。\n" +
                                             "連續／限時／天候／緊急任務即使已選取也不會套用。");
                        }
                        if (EnableRelicXp)
                        {
                            bool OnlySelected = C.XPRelicOnlyEnabled;
                            if (ImGui.Checkbox("僅執行選取的任務", ref OnlySelected))
                            {
                                C.XPRelicOnlyEnabled = OnlySelected;
                                C.Save();
                            }
                            if (C.ShowManualMode)
                            {
                                bool IgnoreManual = C.XPRelicIgnoreManual;
                                if (ImGui.Checkbox("忽略手動模式任務", ref IgnoreManual))
                                {
                                    C.XPRelicIgnoreManual = IgnoreManual;
                                    C.Save();
                                }
                            }
                        }
                    }

                    if (C.ShowCompletionWindow && completionExpanded)
                    {
                        ImGui.TableNextColumn();
                        bool showSelectedJobOnly = C.ShowSelectedJobOnly;
                        if (ImGui.Checkbox("僅顯示選取的職業", ref showSelectedJobOnly))
                        {
                            C.ShowSelectedJobOnly = showSelectedJobOnly;
                            if (showSelectedJobOnly)
                                C.ShowCompletionOnlyJob = false;
                            C.Save();
                        }

                        bool nonGold = C.ShowCompletion_MissingGold;
                        if (ImGui.Checkbox("僅顯示尚未取得金級評價的任務", ref nonGold))
                        {
                            C.ShowCompletion_MissingGold = nonGold;
                            C.Save();
                        }
                    }
                }

                ImGui.EndTable();
            }

            using (var bodyChild = ImRaii.Child("##modeSelect_Body", new Vector2(0, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (!bodyChild.Success) return;

                foreach (var missionType in modeSelect_TableInfo.missionList)
                {
                    missionType.Value.Clear();
                }

                foreach (var mission in CosmicHelper.SheetMissionDict)
                {
                    var Jobs = mission.Value.Jobs;
                    var territoryId = mission.Value.TerritoryId;
                    uint selectedJob = C.SelectedJob;
                    bool sinusEnabled = C.ShowSinusMissions;
                    bool phaennaEnabled = C.ShowPhaennaMissions;

                    if (C.ShowCompletionWindow)
                    {
                        if (C.ShowCompletionOnlyJob)
                        {
                            if (!Jobs.Contains(selectedJob))
                                continue;
                        }
                    }
                    else if (C.GrindProvisionals)
                    {
                        // honestly do nothing here, this is just to catch and show all the jobs for this mode here. Kinda lazy I realize but *-shrugs-*
                    }
                    else if (!Jobs.Contains(selectedJob))
                        continue;


                    if (!sinusEnabled && territoryId == 1237)
                        continue;

                    if (!phaennaEnabled && territoryId == 1291)
                        continue;

                    if (C.GrindProvisionals)
                    {
                        bool provisional = mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather)
                                        || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                                        || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential);

                        if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            modeSelect_TableInfo.missionList["Weather"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            modeSelect_TableInfo.missionList["Timed"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            modeSelect_TableInfo.missionList["Sequence"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled && provisional)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                    else
                    {
                        if (mission.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            modeSelect_TableInfo.missionList["Critical"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            modeSelect_TableInfo.missionList["Weather"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            modeSelect_TableInfo.missionList["Timed"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            modeSelect_TableInfo.missionList["Sequence"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank > 3)
                            modeSelect_TableInfo.missionList["ARank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 3)
                            modeSelect_TableInfo.missionList["BRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 2)
                            modeSelect_TableInfo.missionList["CRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 1)
                            modeSelect_TableInfo.missionList["DRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                }

                int criticalEnabled = modeSelect_TableInfo.missionList.ContainsKey("Critical") ? modeSelect_TableInfo.missionList["Critical"].Count(mission => mission.enabled) : 0;
                int sequenceEnabled = modeSelect_TableInfo.missionList.ContainsKey("Sequence") ? modeSelect_TableInfo.missionList["Sequence"].Count(mission => mission.enabled) : 0;
                int weatherEnabled = modeSelect_TableInfo.missionList.ContainsKey("Weather") ? modeSelect_TableInfo.missionList["Weather"].Count(mission => mission.enabled) : 0;
                int timedEnabled = modeSelect_TableInfo.missionList.ContainsKey("Timed") ? modeSelect_TableInfo.missionList["Timed"].Count(mission => mission.enabled) : 0;
                int aRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("ARank") ? modeSelect_TableInfo.missionList["ARank"].Count(mission => mission.enabled) : 0;
                int bRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("BRank") ? modeSelect_TableInfo.missionList["BRank"].Count(mission => mission.enabled) : 0;
                int cRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("CRank") ? modeSelect_TableInfo.missionList["CRank"].Count(mission => mission.enabled) : 0;
                int dRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("DRank") ? modeSelect_TableInfo.missionList["DRank"].Count(mission => mission.enabled) : 0;
                int allEnabled = modeSelect_TableInfo.missionList.ContainsKey("All Enabled") ? modeSelect_TableInfo.missionList["All Enabled"].Count(mission => mission.enabled) : 0;

                float scrollbarSize = ImGui.GetStyle().ScrollbarSize;
                float buttonRowHeight = (ImGui.GetTextLineHeight() + 8 * scale + 4 * scale) + scrollbarSize;

                ImGui.TextDisabled("顯示分類（可同時展開多個）");
                using (var missionButtons = ImRaii.Child("##tab_scroll", new Vector2(0, buttonRowHeight), false, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (!missionButtons.Success)
                        return;

                    if (C.GrindProvisionals)
                    {
                        ImGui_Tools.DrawCategoryButton($"All Enabled [{allEnabled}]", "main_AllEnabled");
                        ImGui_Tools.DrawCategoryButton($"Sequence [{sequenceEnabled}]", "main_Sequence");
                        ImGui_Tools.DrawCategoryButton($"Weather [{weatherEnabled}]", "main_Weather");
                        ImGui_Tools.DrawCategoryButton($"Timed [{timedEnabled}]", "main_Timed");
                        ImGui_Tools.EndCategoryButtonRow();
                    }
                    else
                    {
                        ImGui_Tools.DrawCategoryButton($"All Enabled [{allEnabled}]", "main_AllEnabled");
                        ImGui_Tools.DrawCategoryButton($"Critical [{criticalEnabled}]", "main_Critical");
                        ImGui_Tools.DrawCategoryButton($"Sequence [{sequenceEnabled}]", "main_Sequence");
                        ImGui_Tools.DrawCategoryButton($"Weather [{weatherEnabled}]", "main_Weather");
                        ImGui_Tools.DrawCategoryButton($"Timed [{timedEnabled}]", "main_Timed");
                        ImGui_Tools.DrawCategoryButton($"A Rank [{aRankEnabled}]", "main_ARank");
                        ImGui_Tools.DrawCategoryButton($"B Rank [{bRankEnabled}]", "main_BRank");
                        ImGui_Tools.DrawCategoryButton($"C Rank [{cRankEnabled}]", "main_CRank");
                        ImGui_Tools.DrawCategoryButton($"D Rank [{dRankEnabled}]", "main_DRank", spacingAfter: 0);
                        ImGui_Tools.EndCategoryButtonRow();
                    }
                }

                if (C.ShowExtraMissionInfo)
                {
                    if (ImGui.BeginTable("Mission Info | Extra Details", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable, Vector2.Zero))
                    {
                        ImGui.TableSetupColumn("Mission Selection Viewer", ImGuiTableColumnFlags.WidthFixed, 200f);
                        ImGui.TableSetupColumn("Specific Mission Info", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        MissionTableInfo();

                        ImGui.TableNextColumn();
                        using (var missionInfoChild = ImRaii.Child("##modeSelect_MissionInfo", new Vector2(0, 0), false))
                        {
                            modeSelect_TableInfo.DrawMissionDetails();
                        }

                        ImGui.EndTable();
                    }
                }
                else
                {
                    MissionTableInfo();
                }
            }
        }

        private static void MissionTableInfo()
        {
            using (var missionTableChild = ImRaii.Child("##modeSelect_MissionTables", new Vector2(0, 0), false))
            {
                var enabledTabs = ImGui_Tools.CategoryStates;
                if (C.GrindProvisionals)
                {
                    modeSelect_TableInfo.missionList["All Enabled"] = modeSelect_TableInfo.missionList["All Enabled"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList(); // ToList() if you need a List<T>, otherwise the IOrderedEnumerable is fine

                    modeSelect_TableInfo.missionList["Sequence"] = modeSelect_TableInfo.missionList["Sequence"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    modeSelect_TableInfo.missionList["Weather"] = modeSelect_TableInfo.missionList["Weather"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    modeSelect_TableInfo.missionList["Timed"] = modeSelect_TableInfo.missionList["Timed"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    if (enabledTabs["main_AllEnabled"])
                        modeSelect_TableInfo.DrawMissionTablev2("All Enabled", "All_Enabled", modeSelect_TableInfo.missionList["All Enabled"]);
                    if (enabledTabs["main_Sequence"])
                        modeSelect_TableInfo.DrawMissionTablev2("Sequence", "Sequence_Missions", modeSelect_TableInfo.missionList["Sequence"]);
                    if (enabledTabs["main_Weather"])
                        modeSelect_TableInfo.DrawMissionTablev2("Weather", "Weather_Missions", modeSelect_TableInfo.missionList["Weather"]);
                    if (enabledTabs["main_Timed"])
                        modeSelect_TableInfo.DrawMissionTablev2("Timed", "Timed_Missions", modeSelect_TableInfo.missionList["Timed"]);
                }
                else
                {
                    if (enabledTabs["main_AllEnabled"])
                    {
                        if (modeSelect_TableInfo.missionList["All Enabled"].Count > 0)
                        {
                            modeSelect_TableInfo.DrawMissionTablev2("All Enabled", "All_Enabled", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["All Enabled"]));
                        }
                        else
                        {
                            ImGui.Text("請先啟用任務，才能在此顯示內容。");
                        }
                    }
                    if (enabledTabs["main_Critical"])
                        modeSelect_TableInfo.DrawMissionTablev2("Critical", "Critical_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Critical"]));
                    if (enabledTabs["main_Sequence"])
                        modeSelect_TableInfo.DrawMissionTablev2("Sequence", "Sequence_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Sequence"]));
                    if (enabledTabs["main_Weather"])
                        modeSelect_TableInfo.DrawMissionTablev2("Weather", "Weather_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Weather"]));
                    if (enabledTabs["main_Timed"])
                        modeSelect_TableInfo.DrawMissionTablev2("Timed", "Timed_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Timed"]));
                    if (enabledTabs["main_ARank"])
                        modeSelect_TableInfo.DrawMissionTablev2("A Rank", "A_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["ARank"]));
                    if (enabledTabs["main_BRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("B Rank", "B_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["BRank"]));
                    if (enabledTabs["main_CRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("C Rank", "C_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["CRank"]));
                    if (enabledTabs["main_DRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("D Rank", "D_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["DRank"]));
                }
            }
        }
    }
}
