using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic;
using ICE.Utilities.GatheringHelper;
using System.Collections.Generic;
using static MissionTimer;

namespace ICE.Ui.MainUi.ModeSelect
{
    internal class modeSelect_TableInfo
    {
        public static HashSet<string> selectedTabs = new HashSet<string>();
        public static uint selectedMission = 0;
        public static List<string> JokeList = new()
        {
            "What is a pirates favorite letter?\n" +
            "You might thing it's R, but tis first love was the C\n" +
            "(It helps if you verbally say it like a pirate)",

            "You know, I was reading this book about anti-gravity recently,\n" +
            "and honestly I'm having a hard time putting it down",

            "Why are tennis pros always hugging each other?\n" +
            "Because they start their match at \"Love All\"",

            "Why can't ghost have babies?\n" +
            "Because they have hallow-eenies",

            "How do you save a drowning pirate?\n" +
            "You give him Cprrrrrr",

            "What is a skeleton's favorite snack?\n" +
            "Ribs! Spare Ribs!",

            "Honestly, just wanted to say thank you for using my plugin, you're appreciated <3",


        };
        public static int jokeId = 0;

        public static Dictionary<string, bool> headerStates = new();

        public static Dictionary<string, List<Mission>> missionList = new()
        {
            ["Critical"] = new List<Mission>(),
            ["Weather"] = new List<Mission>(),
            ["Timed"] = new List<Mission>(),
            ["Sequence"] = new List<Mission>(),
            ["ARank"] = new List<Mission>(),
            ["BRank"] = new List<Mission>(),
            ["CRank"] = new List<Mission>(),
            ["DRank"] = new List<Mission>(),
            ["All Enabled"] = new List<Mission>(),
        };

        public class Mission
        {
            public uint id;
            public bool enabled;
        }

        public static List<Mission> SortMissionList(List<Mission> missions)
        {
            int sortOption = C.TableSortOption;
            var missionInfo = CosmicHelper.SheetMissionDict;

            switch (sortOption)
            {
                case 0: // Sorting by Id
                    return missions.ToList();
                case 1: // Name 
                    return missions.OrderBy(m => missionInfo[m.id].Name).ToList();
                case 2: // Cosmo Credits
                    return missions.OrderByDescending(m => missionInfo[m.id].CosmoCredit).ToList();
                case 3: // Lunar Credits
                    return missions.OrderByDescending(m => missionInfo[m.id].LunarCredit).ToList();
                case 4: // Exp Type 1:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 1)
                                                     .Sum(exp => exp.Value)).ToList();
                case 5: // Exp Type 2:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 2)
                                                     .Sum(exp => exp.Value)).ToList();
                case 6: // Exp Type 3:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 3)
                                                     .Sum(exp => exp.Value)).ToList();
                case 7: // Exp Type 4:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 4)
                                                     .Sum(exp => exp.Value)).ToList();
                case 8: // Exp Type 5:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 5)
                                                     .Sum(exp => exp.Value)).ToList();
                case 9: // Map Location
                    return missions.OrderBy(m => missionInfo[m.id].MarkerId).ToList();
                case 10: // Mission Score
                    return missions.OrderByDescending(m => missionInfo[m.id].ClassScore).ToList();
                default:
                    return missions.ToList();
            }
        }

        public static void DrawCollapsibleHeader(string id, string label, float spacing = 4f, Vector4? borderColor = null, Vector4? backgroundColor = null)
        {
            const float padding = 6.0f;
            const float borderRadius = 2.0f;

            // Initialize header state if needed
            if (!headerStates.ContainsKey(id))
                headerStates[id] = false;

            // Calculate dimensions
            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var windowWidth = ImGui.GetContentRegionAvail().X;
            var textSize = ImGui.CalcTextSize(label);
            var bgHeight = textSize.Y + padding * 2;

            // Define header bounds
            var headerRectMin = cursorPos;
            var headerRectMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + bgHeight);

            // Use provided colors or defaults
            var bgColor = backgroundColor ?? new Vector4(0.2f, 0.2f, 0.2f, 1f);
            var borderCol = borderColor ?? ImGuiColors.ParsedGold;

            // Draw background and border
            drawList.AddRectFilled(headerRectMin, headerRectMax, ImGui.GetColorU32(bgColor), borderRadius);
            drawList.AddRect(headerRectMin, headerRectMax, ImGui.GetColorU32(borderCol), borderRadius);

            // Draw centered label
            var textPos = new Vector2(
                cursorPos.X + (windowWidth - textSize.X) * 0.5f,
                cursorPos.Y + padding
            );
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), label);

            // Handle interaction
            ImGui.SetCursorScreenPos(cursorPos);
            ImGui.PushID(id);
            ImGui.InvisibleButton("##header", new Vector2(windowWidth, bgHeight));
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                headerStates[id] = !headerStates[id];
            ImGui.PopID();

            // Move cursor past header
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + bgHeight + spacing));
        }

        public static void DrawCollapsibleSection(string id, string label, int enabled, List<Mission> missions)
        {
            DrawCollapsibleHeader(id, $"{label} | Enabled: {enabled}");
            if (headerStates.TryGetValue(id, out var isOpen) && isOpen)
            {
                // MissionInfoV2(id, SortMissionList(missions));
            }
        }

        public static bool DrawTabButton(string label, string tabIndex)
        {
            if (selectedTabs.Contains(tabIndex))
            {
                var activeColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive];
                ImGui.PushStyleColor(ImGuiCol.Button, activeColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, activeColor);
            }

            bool clicked = ImGui.Button(label);

            if (selectedTabs.Contains(tabIndex))
                ImGui.PopStyleColor(2);

            if (clicked)
            {
                if (selectedTabs.Contains(tabIndex))
                    selectedTabs.Remove(tabIndex);
                else
                    selectedTabs.Add(tabIndex);
            }

            return clicked;
        }

        public static unsafe void DrawMissionTablev2(string headerName, string tableName, List<Mission> missions)
        {
            // Setting up the size of the table here, ideally I want this to be the width of the current available space
            // Also setting up the header for here as well
            var availableSpace = ImGui.GetContentRegionAvail().X;
            var textSize = ImGui.CalcTextSize(headerName);
            var headerPadding = new Vector2(10, 5);
            var headerHeight = textSize.Y + headerPadding.Y * 2;

            // Custom header to display above the table. This is moreso for quick user viewability
            using (ImRaii.Child($"Table Header: {headerName}", new Vector2(availableSpace, headerHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var centeredPosX = (availableSpace - textSize.X) / 2;

                ImGui.SetCursorPosY(headerPadding.Y);
                ImGui.SetCursorPosX(centeredPosX);
                ImGui.Text($"{headerName} 任務");
            }

            // Table settings, just so I can sort it out visibly vs... being shoved in the table
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                ImGuiTableFlags.Borders |
                                ImGuiTableFlags.Reorderable |
                                ImGuiTableFlags.Hideable |
                                ImGuiTableFlags.SizingFixedFit;
            int tableTotalColumns = 18; // How many columns am I using. 

            // This is here to auto show/hide specific columns that might not be necessary (gathering profiles, and planetary tokens as Ex.)
            bool hasGathering = false;
            bool hasToken = false;

            foreach (var mission in missions)
            {
                var id = mission.id;
                var missionInfo = CosmicHelper.SheetMissionDict[id];

                if (missionInfo.Jobs.Overlaps(CosmicHelper.GatheringJobList))
                    hasGathering = true;
                if (missionInfo.RewardItemAmount > 0)
                    hasToken = true;
            }

            if (ImGui.BeginTable($"MissionList_{tableName}", tableTotalColumns, tableFlags))
            {
                #region Table Column Setup

            ImGui.TableSetupColumn("啟用"); // 0
            ImGui.TableSetupColumn("職業");
            ImGui.TableSetupColumn("手動");
                ImGui.TableSetupColumn("ID");
                ImGui.TableSetupColumn("✓");
            ImGui.TableSetupColumn("任務名稱");
            ImGui.TableSetupColumn("宇宙");
            ImGui.TableSetupColumn("星球");
            ImGui.TableSetupColumn("分數");
            ImGui.TableSetupColumn("報酬物品"); // 9

                // Xp Columns Here
                float padding = 10f;
                float xpWidth = ImGui.CalcTextSize("III").X + padding;
                ImGui.TableSetupColumn("I"); // 10
                ImGui.TableSetupColumn("II");
                ImGui.TableSetupColumn("III");
                ImGui.TableSetupColumn("IV");
                ImGui.TableSetupColumn("V"); // 14

            ImGui.TableSetupColumn("繳交模式"); // 15
            ImGui.TableSetupColumn("採集設定檔");
            ImGui.TableSetupColumn("任務備註"); // 17

                #endregion

                #region Auto-Hiding Columns

                ImGui.TableSetColumnEnabled(1, (C.ShowCompletionWindow || C.GrindProvisionals)); // Job Column (Useful for provisionals/Timed)
                ImGui.TableSetColumnEnabled(2, C.ShowManualMode);

                if (C.Auto_ShowTokens)
                {
                    ImGui.TableSetColumnEnabled(9, hasToken);
                }

                ImGui.TableSetColumnEnabled(16, hasGathering);

                #endregion

                #region Custom Header Stuff

                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                int columnIndexCount = 0;

                #region Enabled Column

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("啟用");
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.OpenPopup("Enabled Options");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("啟用或停用任務自動化");
            ImGui.Text("按一下左鍵開啟選項");
                    ImGui.EndTooltip();
                }
                if (ImGui.BeginPopup("Enabled Options"))
                {
            if (ImGui.Button("全部啟用"))
                    {
                        foreach (var mission in missions)
                        {
                            C.MissionConfig[mission.id].Enabled = true;
                            if (GetOnlyPreviousMissionsRecursive(mission.id).Count > 0)
                            {
                                foreach (var prevMission in GetOnlyPreviousMissionsRecursive(mission.id))
                                {
                                    var prevMissionConfig = C.MissionConfig[prevMission];
                                    prevMissionConfig.Enabled = true;
                                }
                            }
                        }
                        C.Save();
                    }

            if (ImGui.Button("全部停用"))
                    {
                        foreach (var mission in missions)
                        {
                            C.MissionConfig[mission.id].Enabled = false;
                        }
                        C.Save();
                    }

                    ImGui.EndPopup();
                }
                columnIndexCount++;

                #endregion

                #region Jobs

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("職業");
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.OpenPopup("Jobs Options");
                }
                if (ImGui.BeginPopup("Jobs Options"))
                {
                    bool showAllJobs = C.ShowCompletionOnlyJob;
            if (ImGui.RadioButton("顯示所有職業", !showAllJobs))
                    {
                        C.ShowCompletionOnlyJob = false;
                        C.Save();
                    }
            if (ImGui.RadioButton("只顯示目前職業", showAllJobs))
                    {
                        C.ShowCompletionOnlyJob = true;
                        C.Save();
                    }
                    ImGui.EndPopup();
                }
                columnIndexCount++;

                #endregion

                #region Manual

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("手動");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("手動模式－需要使用者操作");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region ID

                ImGui.TableSetColumnIndex(columnIndexCount);
                ImGui.TableHeader("ID");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("任務 ID");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Completed

                ImGui.TableSetColumnIndex(columnIndexCount);
                ImGui.TableHeader("✓");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("任務完成狀態");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Mission Name

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("任務名稱");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("點擊任務名稱查看詳細資訊");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Cosmocredits

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("宇宙");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("宇宙信用點報酬");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Planetary Credits

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("星球");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("星球信用點報酬");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Score

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("技巧點");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("職業技巧點報酬");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Planet Tokens

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("代幣");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("此任務可獲得的代幣");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Relic XP

                string[] xpLabels = { "I", "II", "III", "IV", "V" };
                for (int i = 0; i < 5; i++)
                {
                    ImGui.TableSetColumnIndex(columnIndexCount);
                    ImGui.TableHeader(xpLabels[i]);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                ImGui.Text($"宇宙工具經驗值類型 {xpLabels[i]} 報酬");
                        ImGui.EndTooltip();
                    }
                    columnIndexCount++;
                }

                #endregion

                #region Turnin Mode

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("繳交模式");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("設定任務繳交選項");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Gathering Profile

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("採集設定檔");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("選擇採集任務使用的設定檔");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #region Mission Notes

                ImGui.TableSetColumnIndex(columnIndexCount);
        ImGui.TableHeader("任務備註");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
            ImGui.Text("其他任務資訊與需求");
                    ImGui.EndTooltip();
                }
                columnIndexCount++;

                #endregion

                #endregion

                foreach (var entry in missions)
                {
                    var Id = entry.id;
                    var missionConfig = C.MissionConfig[Id];
                    var missionInfo = CosmicHelper.SheetMissionDict[Id];

                    bool unsupported = UnsupportedMissions.Ids.Contains(Id);
                    bool hideUnsupported = C.HideUnsupportedMissions;

                    if (unsupported && hideUnsupported)
                        continue;

                    if (C.ShowCompletionWindow)
                    {
                        if (C.ShowCompletion_MissingGold)
                        {
                            var managerPtr = WKSManager.Instance();
                            if (managerPtr == null) continue;

                            var manager = (WKSManagerCustom*)managerPtr;
                            var isGold = manager->IsMissionGolded(Id);

                            if (isGold)
                                continue;
                        }
                        if (C.ShowSelectedJobOnly && !CosmicHelper.SheetMissionDict[Id].Jobs.Contains(C.SelectedJob))
                        {
                            continue;
                        }
                    }

                    ImGui.TableNextRow();
                    ImGui.PushID(Id);

                    #region Enabled Column Stuff

                    ImGui.TableSetColumnIndex(0);
                    bool enabled = missionConfig.Enabled;
                    if (Table_CenterCheckbox("##EnableMission", ref enabled))
                    {
                        missionConfig.Enabled = enabled;
                        if (missionConfig.Enabled == true)
                        {
                            if (GetOnlyPreviousMissionsRecursive(Id).Count >0)
                            {
                                foreach (var prevMission in GetOnlyPreviousMissionsRecursive(Id))
                                {
                                    var prevMissionConfig = C.MissionConfig[prevMission];
                                    prevMissionConfig.Enabled = true;
                                }
                            }
                        }

                        C.Save();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }

                    #endregion

                    #region Job Info

                    ImGui.TableNextColumn();
                    if (missionInfo.Jobs.Count > 1)
                    {
                        ISharedImmediateTexture? job1Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.First()];
                        ISharedImmediateTexture? job2Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.Last()];
                        Vector2 imageSize = new Vector2(23, 23);

                        ImGui.Image(job1Icon.GetWrapOrEmpty().Handle, imageSize);
                        ImGui.SameLine(0, 2);
                        ImGui.Image(job2Icon.GetWrapOrEmpty().Handle, imageSize);
                    }
                    else
                    {
                        ISharedImmediateTexture? job1Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.First()];
                        Vector2 imageSize = new Vector2(23, 23);
                        ImGui.Image(job1Icon.GetWrapOrEmpty().Handle, imageSize);
                    }

                    #endregion

                    #region Manual Mode

                    ImGui.TableNextColumn();
                    bool manualMode = missionConfig.ManualMode;
                    if (Table_CenterCheckbox("##Manual Mode", ref manualMode))
                    {
                        missionConfig.ManualMode = manualMode;
                        C.Save();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }

                    #endregion

                    #region Mission Id

                    ImGui.TableNextColumn();
                    Table_FullCenterText(Id.ToString());

                    #endregion

                    #region Completion Status

                    ImGui.TableNextColumn();
                    CompletionStatus_Formatted(Id);

                    #endregion

                    #region Mission Name + Flag Info

                    ImGui.TableNextColumn();
                    if (unsupported)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f)); // Red color (RGBA)
            ImGuiEx.IconWithTooltip(FontAwesomeIcon.ExclamationTriangle, "目前尚未支援，正在開發中。");
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                    }
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ExpertCraft))
                    {
                        if (EzThrottler.Throttle("Throttling the manip update every couple of seconds", 1000))
                            PlayerHelper.UpdateHasManip();

                        var crafterJobId = missionInfo.Jobs.Where(x => CosmicHelper.CrafterJobList.Contains(x)).FirstOrDefault();
                        if (PlayerHelper.ManipClassInfo.TryGetValue(crafterJobId, out var manipInfo) && !manipInfo.HasUnlocked)
                        {
                            var color = EColor.Yellow;
                            ImGuiEx.IconWithTooltip(color, FontAwesomeIcon.ExclamationTriangle, 
                    "依遊戲判定，這是專家製作，但目前職業尚未解鎖掌握。\n" +
                    "你可以自行啟用，但解鎖該技能前 Artisan 不會允許製作。\n" +
                    "可改用巨集，或先完成約 68 級的職業任務。");
                        }
                        ImGui.SameLine();
                    }

                    ImGui.Text(missionInfo.Name);
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }
                    if (missionInfo.MarkerId != 0)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Flag.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemClicked())
                        {
                            selectedMission = Id;
                            Utils.SetGatheringRing(missionInfo.TerritoryId, (int)missionInfo.MapPosition.X, (int)missionInfo.MapPosition.Y, missionInfo.Radius, missionInfo.Name);
                        }
                    }
                    if (GatheringUtil.CriticalLocations.TryGetValue(Id, out var criticalLoc))
                    {
                        ImGui.SameLine();
                        ImGuiEx.Icon(FontAwesomeIcon.FlagCheckered);
                        if (ImGui.IsItemClicked())
                        {
                            Utils.SetFlagForNPC(missionInfo.TerritoryId, criticalLoc.MapInfo.X, criticalLoc.MapInfo.Y);
                        }
                    }

                    #endregion

                    #region Cosmo | Planetary | Class Score | Tokens

                    ImGui.TableNextColumn();
                    Table_FullCenterText(missionInfo.CosmoCredit.ToString());

                    ImGui.TableNextColumn();
                    Table_FullCenterText(missionInfo.LunarCredit.ToString());

                    ImGui.TableNextColumn();
                    Table_FullCenterText(missionInfo.ClassScore.ToString());

                    ImGui.TableNextColumn();
                    string itemAmount = missionInfo.RewardItemAmount > 0 ? $"{missionInfo.RewardItemAmount}" : "-";
                    Table_FullCenterText(itemAmount);

                    #endregion

                    #region Relic Xp Info

                    for (int i = 1; i < 6; i++)
                    {
                        ImGui.TableNextColumn();
                        var expReward = missionInfo.RelicXpInfo.Where(exp => exp.Key == i).FirstOrDefault();
                        var relicXp = expReward.Value.ToString();

                        if (relicXp == "0")
                        {
                            relicXp = "-";
                        }

                        Table_FullCenterText(relicXp);
                    }

                    #endregion

                    #region Mission Turnins

                    ImGui.TableNextColumn();
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining))
                    {
        Table_FullCenterText("自動");
                        if (missionConfig.AutoTurnin == false)
                        {
                            missionConfig.AutoTurnin = true;
                            missionConfig.TurninGold = false;
                            missionConfig.TurninSilver = false;
                            missionConfig.TurninBronze = false;

                            C.Save();
                        }
                    }
                    else
                    {
        if (Table_CenteredButton("選擇繳交方式"))
                        {
                            ImGui.OpenPopup("Mission Turnin Settings");
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            if (missionConfig.AutoTurnin)
            ImGui.Text("自動繳交－啟用");
                            else
                            {
                                if (missionConfig.TurninGold)
            ImGui.Text("已啟用金牌");
                                if (missionConfig.TurninSilver)
            ImGui.Text("已啟用銀牌");
                                if (missionConfig.TurninBronze)
            ImGui.Text("已啟用銅牌");
                            }

                            ImGui.EndTooltip();
                        }

                        if (ImGui.BeginPopup("Mission Turnin Settings"))
                        {
                            bool anyTurnin = missionConfig.AutoTurnin;
                            bool goldTurnin = missionConfig.TurninGold;
                            bool silverTurnin = missionConfig.TurninSilver;
                            bool bronzeTurnin = missionConfig.TurninBronze;

            ImGui.Text("選擇繳交選項");
                            ImGui.Dummy(new Vector2(0, 2));

            if (ImGui.Checkbox("自動", ref anyTurnin))
                            {
                                if (anyTurnin)
                                {
                                    missionConfig.TurninGold = false;
                                    missionConfig.TurninSilver = false;
                                    missionConfig.TurninBronze = false;

                                    missionConfig.AutoTurnin = anyTurnin;
                                }
                                else
                                {
                                    if (!(bronzeTurnin && silverTurnin && goldTurnin))
                                    {
                                        missionConfig.AutoTurnin = true;
                                    }
                                }

                                C.Save();
                            }
            ImGuiEx.HelpMarker("此選項會盡量取得最佳成果，但必要時會繳交任何成果且不停止。");

                            ImGui.Separator();

            if (ImGui.Checkbox("金牌", ref goldTurnin))
                            {
                                if (anyTurnin && goldTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninGold = goldTurnin;
                                C.Save();
                            }
            if (ImGui.Checkbox("銀牌", ref silverTurnin))
                            {
                                if (anyTurnin && silverTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninSilver = silverTurnin;
                                C.Save();
                            }
            if (ImGui.Checkbox("銅牌", ref bronzeTurnin))
                            {
                                if (anyTurnin && bronzeTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninBronze = bronzeTurnin;
                                C.Save();
                            }

                            ImGui.EndPopup();
                        }
                    }

                    #endregion

                    #region Gathering Profile Settings

                    bool gatherProfile = missionInfo.Attributes.HasFlag(MissionAttributes.Gather);
                    bool collectable = missionInfo.Attributes.HasFlag(MissionAttributes.Collectables) || missionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);

                    ImGui.TableNextColumn();
                    if (gatherProfile && !collectable)
                    {
                        string profileName = "???";
                        if (C.GatherProfiles.TryGetValue(missionConfig.GProfileId, out var profileSetting))
                        {
                            profileName = profileSetting.Name;
                        }
                        else
                        {
                            profileName = "???";
                        }

                        if (Table_CenteredButton($"{profileName}"))
                        {
                            ImGui.OpenPopup("Selecting Gathering Profile");
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
        ImGui.Text("選擇要使用的設定檔");
                            ImGui.EndTooltip();
                        }
                        if (ImGui.BeginPopup("Selecting Gathering Profile"))
                        {
            ImGui.Text($"目前選擇：{profileName}");
                            ImGui.Separator();

                            foreach (var profile in C.GatherProfiles)
                            {
                                var id = profile.Key;
                                bool profileSelected = missionConfig.GProfileId == id;
                                ImGui.PushID($"{id}_{profile.Value.Name}");
                                if (ImGui.RadioButton(profile.Value.Name, profileSelected))
                                {
                                    missionConfig.GProfileId = id;
                                    C.Save();
                                }
                                ImGui.PopID();
                            }

                            ImGui.EndPopup();
                        }
                    }
                    else if (gatherProfile && collectable)
                    {
        Table_FullCenterText("自動");
                    }
                    else if (missionInfo.Attributes.HasFlag(MissionAttributes.Fish))
                    {
        if (Table_CenteredButton("選擇設定檔"))
                        {
                            ImGui.OpenPopup("Select Fishing Profile");
                        }
                        if (ImGui.BeginPopup("Select Fishing Profile"))
                        {
            ImGui.Text($"捕魚設定檔：{missionInfo.Name}");
                            ImGui.Separator();
                            bool builtInPreset = missionConfig.Use_BuildinPreset;
            if (ImGui.Checkbox("使用內建預設", ref builtInPreset))
                            {
                                missionConfig.Use_BuildinPreset = builtInPreset;
                                C.Save();
                            }
            ImGuiEx.HelpMarker("啟用後，AutoHook 會使用插件隨附的預設。\n" +
                "若要使用 AutoHook 中已有的預設，請取消勾選並在下方輸入名稱。");
                            using (ImRaii.Disabled(builtInPreset))
                            {
                                string presetName = missionConfig.AutoHookPresetName;
                                ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("預設名稱", ref presetName))
                                {
                                    missionConfig.AutoHookPresetName = presetName;
                                    C.Save();
                                }
                            }

                            ImGui.EndPopup();
                        }
                    }

                    #endregion

                    #region Notes

                    ImGui.TableNextColumn();
                    int notesCount = 0;

                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                    {
                        Table_FontCenter(FontAwesomeIcon.Clock);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{missionInfo.StartTime}:00 - {missionInfo.EndTime-1}:59");
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                    {
                        Table_FontCenter(FontAwesomeIcon.ListOl);
                        if (ImGui.IsItemHovered())
                        {
                            var prevMissions = GetOnlyPreviousMissionsRecursive(Id);

                            ImGui.BeginTooltip();
        ImGui.Text("連續任務");
                            ImGui.Separator();
                            for (int i = 0; i < prevMissions.Count; i++)
                            {
                                var prevMission = prevMissions[i];
                                ImGui.Text($"{i + 1}: [{prevMission}] - {CosmicHelper.SheetMissionDict[prevMission].Name}");
                            }
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine(0, 2);

                        if (CosmicHelper.WeatherIds.ContainsKey(missionInfo.Weather))
                        {
                            ISharedImmediateTexture? weatherIcon = CosmicHelper.WeatherIconDict[missionInfo.Weather];
                            Vector2 ImageSize = new Vector2(23, 23);
                            ImGui.Image(weatherIcon.GetWrapOrEmpty().Handle, ImageSize);
                        }
                        else
                        {
                            Table_FontCenter(FontAwesomeIcon.Cloud);
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
            ImGui.Text($"天候：{missionInfo.Weather}");
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (CosmicHelper.MissionUnlock.TryGetValue(Id, out var unlock))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine(0, 2);

                        if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                        {
                            if (tex.TryGetWrap(out var wrap, out var exc))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(23, 23), new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                            }
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
            ImGui.Text("必須先在下列任務取得金牌，才能執行此任務：");
                            foreach (var mission in unlock)
                            {
                                CompletionStatus_Normal(mission);
                                ImGui.SameLine();
                                ImGui.Text($"[{mission}] - {CosmicHelper.SheetMissionDict[mission].Name}");
                            }
                            ImGui.EndTooltip();
                        }
                        notesCount++;

                    }
                    if (missionInfo.Jobs.Count > 1)
                    {
                        if (notesCount > 0)
                            ImGui.SameLine(0, 2);

                        ISharedImmediateTexture? job1Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.First()];
                        ISharedImmediateTexture? job2Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.Last()];
                        Vector2 imageSize = new Vector2(23, 23);

                        ImGui.Image(job1Icon.GetWrapOrEmpty().Handle, imageSize);
                        ImGui.SameLine(0, 2);
                        ImGui.Image(job2Icon.GetWrapOrEmpty().Handle, imageSize);
                        notesCount++;
                    }
                    if (CosmicHelper.CustomMissionNotes.TryGetValue(Id, out var notes))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine();
                        ImGuiEx.Icon(FontAwesomeIcon.Trophy);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text(notes.NoteInfo);
            ImGui.Text($"每分鐘平均分數：{notes.SPM:N2}");

                            ImGui.EndTooltip();
                        }
                    }

                    #endregion

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        public class ScoringInfo
        {
            public int Multipler { get; set; } = 1;
            public double Score { get; set; } = 0;
            public double Cosmocredits { get; set; } = 0;
            public double Planetcredits { get; set; } = 0;
            public int TotalCompleted { get; set; } = 0;
        }
        public static Dictionary<string, ScoringInfo> MissionScores = new()
        {
            ["Critical"] = new(),
            ["Bronze"] = new() { Multipler = 1 },
            ["Silver"] = new() { Multipler = 4 },
            ["Gold"] = new() { Multipler = 5 },
        };

        public static void DrawMissionDetails()
        {
            if (CosmicHelper.SheetMissionDict.TryGetValue(selectedMission, out var mission))
            {
                var id = selectedMission;
                ImGui.PushID($"{mission}_{id}");

                #region Mission Name

        ImGui.Text("任務：");
                ImGui.SameLine(0, 5);
                ImGui.TextDisabled($"[{id}]");
                ImGui.SameLine(0, 5);
                ImGui.Text($"{mission.Name}");

                #endregion

                if (ImGui.BeginTable("Detailed Mission Info", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
                {
            ImGui.TableSetupColumn("名稱");
            ImGui.TableSetupColumn("資訊");

                    // Row 1
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
            ImGui.Text("宇宙信用點");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.CosmoCredit}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
            ImGui.Text("星球信用點");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.LunarCredit}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
            ImGui.Text("職業技巧點：");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.ClassScore}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
            ImGui.Text("職業");

                    ImGui.TableNextColumn();
                    foreach (var job in mission.Jobs)
                    {
                        ISharedImmediateTexture? icon = CosmicHelper.JobIconDict[job];
                        Vector2 size = new Vector2(20, 20);
                        ImGui.Image(icon.GetWrapOrEmpty().Handle, size);
                        ImGui.SameLine();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
            ImGui.Text("已完成：");

                    ImGui.TableNextColumn();
                    CompletionStatus_Normal(selectedMission);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
            ImGui.Text("宇宙工具經驗值類型");

                    ImGui.TableNextColumn();
            ImGui.Text("數量");

                    foreach (var xp in mission.RelicXpInfo.OrderByDescending(x => x.Key))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        string type = "";
                        switch (xp.Key)
                        {
                            case 1:
                                type = "I";
                                break;
                            case 2:
                                type = "II";
                                break;
                            case 3:
                                type = "III";
                                break;
                            case 4:
                                type = "IV";
                                break;
                            case 5:
                                type = "V";
                                break;
                            default:
                                type = "???";
                                break;
                        }

                        ImGui.Text($"Lv. {type}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{xp.Value}");
                    }

                    if (mission.BronzeScore != 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
            ImGui.Text("銅牌需求");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{mission.BronzeScore}");
                    }

                    if (mission.SilverScore != 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
            ImGui.Text("銀牌需求");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{mission.SilverScore}");
                    }

                    if (mission.GoldScore != 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
            ImGui.Text("金牌需求");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{mission.GoldScore}");
                    }

                    if (mission.MarkerId != 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
            ImGui.Text("採集區域");

                        ImGui.TableNextColumn();

                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Flag.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemClicked())
                        {
                            Utils.SetGatheringRing(mission.TerritoryId, (int)mission.MapPosition.X, (int)mission.MapPosition.Y, mission.Radius, mission.Name);
                        }
                    }

                    if (GatheringUtil.CriticalLocations.TryGetValue(selectedMission, out var criticalLoc))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
            ImGui.Text("緊急任務區域");

                        ImGui.TableNextColumn();
                        ImGuiEx.Icon(FontAwesomeIcon.Flag);
                        if (ImGui.IsItemClicked())
                        {
                            Utils.SetFlagForNPC(mission.TerritoryId, criticalLoc.MapInfo.X, criticalLoc.MapInfo.Y);
                        }
                    }

                    ImGui.EndTable();
                }

                if (mission.Crafts_Main.Count > 0)
                {
                    WindowSpacer();

                    var craftCount = 0;
                    var job = mission.Jobs.First(x => CosmicHelper.CrafterJobList.Contains(x));
            ImGui.Text("配方詳細資訊");
                    foreach (var craft in mission.Crafts_Main)
                    {
                        craftCount++;
                        if (Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Recipe>().TryGetRow(craft.Key, out var recipe))
                        {
                            string itemName = recipe.ItemResult.Value.Name.ToString();
                            if (ImGui.CollapsingHeader($"{itemName} [{craftCount}]"))
                            {
                                if (ImGui.BeginTable($"Recipe Detailed Info: {itemName} {craftCount}", 2, ImGuiTableFlags.SizingFixedFit))
                                {
                                    var recipeInfo = CosmicHelper.SpecificRecipeInfo(job, craft.Key);

                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                ImGui.Text("耐久度");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{recipeInfo.Durability}");

                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                ImGui.Text("作業精度");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{recipeInfo.Progress}");

                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                ImGui.Text("最高品質");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{recipeInfo.Quality}");

                                    ImGui.EndTable();
                                }
                                Settings.Settings_Table.CraftSettings.DrawRecipeAssignment(selectedMission, craft.Key);
                            }
                        }
                    }
                }

                WindowSpacer();

            ImGui.Text("任務屬性");
                if (mission.Attributes == MissionAttributes.None)
                {
                ImGui.Text("無");
                    return;
                }
                else
                {
                    foreach (MissionAttributes flag in Enum.GetValues<MissionAttributes>())
                    {
                        if (flag != MissionAttributes.None && mission.Attributes.HasFlag(flag))
                        {
                            ImGui.Text(flag.ToString());
                        }
                    }
                }

                if (CosmicHelper.MissionUnlock.TryGetValue(selectedMission, out var unlock))
                {
            ImGui.Text("必須先在下列任務取得金牌，才能執行此任務：");
                    foreach (var lockedMission in unlock)
                    {
                        CompletionStatus_Normal(lockedMission);
                        ImGui.SameLine();
                        ImGui.Text($"[{lockedMission}] - {CosmicHelper.SheetMissionDict[lockedMission].Name}");
                    }

                }

                WindowSpacer();
        ImGui.Text("任務時間統計");

                if (C.MissionConfig.TryGetValue(selectedMission, out var config))
                {
                    bool allowDelete = (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)) && (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl));

                    using (ImRaii.Disabled(!allowDelete))
                    {
        if (ImGui.Button("重設統計"))
                        {
                            P.MissionTimer.ResetTimers(selectedMission);
                        }
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.BeginTooltip();
            ImGui.Text("按住 Shift＋Ctrl");
                        ImGui.EndTooltip();
                    }

                    if (config.TurninRecords.Count > 0)
                    {
            ImGui.Text($"最佳時間：{TimeSpan.FromSeconds(config.BestTime):mm\\:ss\\.ff}");
            ImGui.Text($"平均時間：{TimeSpan.FromSeconds(config.AverageTime):mm\\:ss\\.ff}");
                    }
                    else
                    {
            ImGui.Text("最佳時間：--:--:--");
            ImGui.Text("平均時間：--:--:--");
                    }

        ImGui.Text($"完成次數：{config.TotalCompletions}");
        ImGui.Text($"限時任務放棄次數：{config.FailedCounters}");

                    if (CosmicHelper.SheetMissionDict.TryGetValue(selectedMission, out var missionInfo))
                    {
                        var baseScore = missionInfo.ClassScore;
                        var comsoCredit = missionInfo.CosmoCredit;
                        var planetCredit = missionInfo.LunarCredit;

                        ImGui.Separator();
        ImGui.Text("每小時預估分數：");
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
        ImGui.Text("估算前提：");
                            ImGui.Text("1: You have immaculate rng of getting the mission you want every time");
                            ImGui.Text("2: You're hitting the threshold every time");
        ImGui.Text("此數值以平均時間計算；請先完成數次任務，讓時間估算更準確。");
                            ImGui.EndTooltip();
                        }

                        bool ShowScorePerMinute = C.ShowSPM;
                        foreach (var kind in MissionScores)
                        {
                            var tier = kind.Key;
                            var info = kind.Value;

                            var averageTime = tier switch
                            {
                                "Critical" => config.AverageTime,
                                "Bronze" => config.AverageBronzeTime,
                                "Silver" => config.AverageSilverTime,
                                "Gold" => config.AverageGoldTime,
                                _ => 0
                            };

                            var totalComplete = tier switch
                            {
                                "Critical" => config.CriticalCompletions,
                                "Gold" => config.GoldCompletions,
                                "Silver" => config.SilverCompletions,
                                "Bronze" => config.BronzeCompletion,
                                _ => 0
                            };

                            info.Score = MissionStatsCalculator.CalculateCurrencyPerMinute(averageTime, baseScore, info.Multipler);
                            info.Cosmocredits = MissionStatsCalculator.CalculateCurrencyPerMinute(averageTime, comsoCredit, info.Multipler);
                            info.Planetcredits = MissionStatsCalculator.CalculateCurrencyPerMinute(averageTime, planetCredit, info.Multipler);
                            info.TotalCompleted = totalComplete;

                            if (!C.ShowSPM)
                            {
                                info.Score *= 60;
                                info.Cosmocredits *= 60;
                                info.Planetcredits *= 60;
                            }
                        }

                        if (mission.Attributes.HasFlag(MissionAttributes.Critical))
                        {
                            var criticalScore = MissionStatsCalculator.CalculateCurrencyPerMinute(config.AverageTime, baseScore, 1.0);
                            if (!C.ShowSPM)
                                criticalScore *= 60;

        string showingX = ShowScorePerMinute ? "每分鐘" : "每小時";
        if (ImGui.Checkbox($"目前顯示：{showingX}分數", ref ShowScorePerMinute))
                            {
                                C.ShowSPM = ShowScorePerMinute;
                                C.Save();
                            }
                            if (ImGui.BeginTable("Critical Scoring Info", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
                            {
            ImGui.TableSetupColumn("繳交");
            ImGui.TableSetupColumn("分數");
            ImGui.TableSetupColumn("宇宙信用點");
            ImGui.TableSetupColumn("星球信用點");

                                ImGui.TableHeadersRow();

                                var entry = MissionScores["Critical"];

                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
            ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), "緊急");

                                ImGui.TableNextColumn();
                                ImGui.Text($"{entry.Score:N2}");

                                ImGui.TableNextColumn();
                                ImGui.Text($"{entry.Cosmocredits:N2}");

                                ImGui.TableNextColumn();
                                ImGui.Text($"{entry.Planetcredits:N2}");

                                ImGui.EndTable();
                            }
                        }
                        else
                        {
                            List<(string type, Vector4 color)> turninTypes = new()
                            {
                                new() { type = "Bronze", color = new Vector4(0.8f, 0.5f, 0.3f, 1.0f)},
                                new() { type = "Silver", color = new Vector4(0.7f, 0.7f, 0.7f, 1.0f)},
                                new() { type = "Gold", color = new Vector4(1.0f, 0.84f, 0.0f, 1.0f)}
                            };
        string showingX = ShowScorePerMinute ? "每分鐘" : "每小時";
        if (ImGui.Checkbox($"目前顯示：{showingX}分數", ref ShowScorePerMinute))
                            {
                                C.ShowSPM = ShowScorePerMinute;
                                C.Save();
                            }
                            if (ImGui.BeginTable("Critical Scoring Info", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
                            {
            ImGui.TableSetupColumn("繳交");
            ImGui.TableSetupColumn("分數");
            ImGui.TableSetupColumn("宇宙信用點");
            ImGui.TableSetupColumn("星球信用點");

                                ImGui.TableHeadersRow();

                                foreach (var type in turninTypes)
                                {
                                    var entry = MissionScores[type.type];
                                    var displayType = type.type switch
                                    {
                                        "Bronze" => "銅牌",
                                        "Silver" => "銀牌",
                                        "Gold" => "金牌",
                                        _ => type.type,
                                    };
                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                                    ImGui.TextColored(type.color, $"{displayType} [{entry.TotalCompleted}]");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{entry.Score:N2}");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{entry.Cosmocredits:N2}");

                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{entry.Planetcredits:N2}");
                                }

                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                ImGui.Text("平均");
                                ImGui.SameLine();
                                ImGui.TextDisabled("?");
                                if (ImGui.IsItemHovered())
                                {
                ImGui.SetTooltip("依目前銅牌／銀牌／金牌的完成比例估算。\n" +
                    "系統會計算所有成果的平均分數，並假設一小時內持續取得相同平均成果，藉此估算單一任務的每小時收益。");
                                }

                                ImGui.TableNextColumn();
                                var score = MissionStatsCalculator.CalculateActualScorePerMinute(config.TurninRecords, baseScore);
                                ImGui.Text($"{score:N2}");

                                ImGui.TableNextColumn();
                                var credits = MissionStatsCalculator.CalculateActualScorePerMinute(config.TurninRecords, comsoCredit);
                                ImGui.Text($"{credits:N2}");

                                ImGui.TableNextColumn();
                                var planet = MissionStatsCalculator.CalculateActualScorePerMinute(config.TurninRecords, planetCredit);
                                ImGui.Text($"{planet:N2}");



                                ImGui.EndTable();
                            }
                            /*
                            var ActualSPM = MissionStatsCalculator.CalculateActualScorePerMinute(config.TurninRecords, baseScore);
                            var bronzeScore = MissionStatsCalculator.CalculateCurrencyPerMinute(config.AverageBronzeTime, baseScore, 1.0);
                            var silverScore = MissionStatsCalculator.CalculateCurrencyPerMinute(config.AverageSilverTime, baseScore, 4.0);
                            var goldScore = MissionStatsCalculator.CalculateCurrencyPerMinute(config.AverageGoldTime, baseScore, 5.0);

                            // Convert to per-hour if needed
                            if (!C.ShowSPM)
                            {
                                ActualSPM *= 60;
                                bronzeScore *= 60;
                                silverScore *= 60;
                                goldScore *= 60;
                            }
        string timeUnit = C.ShowSPM ? "分／分鐘" : "分／小時";
        string actualTimeUnit = C.ShowSPM ? "實際分／分鐘" : "實際分／小時";

        ImGui.Text($"實際{timeUnit}：{ActualSPM:F2}");
                            ImGui.SameLine();
                            ImGui.TextDisabled("?");
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
            ImGui.Text("此估算依目前銅牌／銀牌／金牌的完成比例計算。");
            ImGui.Text("系統會計算所有成果的平均分數，並假設一小時內持續取得相同平均成果。");
                                ImGui.EndTooltip();
                            }
                            if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            {
        string averageSequenceScore = $"連續任務每分鐘平均分數：{MissionStatsCalculator.CalculateAverageSequenceScorePerMinute(id, 5):N2}";
                                ImGui.Text(averageSequenceScore);
                                ImGui.SameLine();
                                ImGui.TextDisabled("?");
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
            ImGui.Text("估算前提：先前所有任務均取得金牌，才能執行此任務。");
            ImGui.Text("此數值方便比較連續任務與重複單一普通任務的平均收益。");
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.TextColored(new Vector4(0.8f, 0.5f, 0.3f, 1.0f), $"Bronze: {bronzeScore:F0} {timeUnit} [{config.BronzeCompletion}/{config.TotalCompletions}]");
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"Silver: {silverScore:F0} {timeUnit} [{config.SilverCompletions}/{config.TotalCompletions}]");
                            ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), $"Gold: {goldScore:F0} {timeUnit} [{config.GoldCompletions}/{config.TotalCompletions}]");
#if DEBUG
                            var creditPerMinute = MissionStatsCalculator.CalculateCurrencyPerMinute(config.AverageGoldTime, mission.CosmoCredit, 5.0);
        ImGui.Text($"每分鐘信用點：{creditPerMinute:N2}");
#endif
                            */
                        }
                    }


        if (config.TurninRecords.Count > 0 && ImGui.CollapsingHeader("查看所有完成時間"))
                    {
                        for (int i = 0; i < config.TurninRecords.Count; i++)
                        {
                            var record = config.TurninRecords[i];

                            ImGui.Text($"[{i+1}] \u2192 {TimeSpan.FromSeconds(record.Time):mm\\:ss\\.ff}");
                            ImGui.SameLine();
                            DrawColoredStar(record.State);

                        }
                    }
                }

                ImGui.PopID();
            }
            else
            {
                string joke = JokeList[jokeId];
                ImGui.TextWrapped(joke);
            }
        }

        #region Table Tools

        private static void WindowSpacer()
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
        }

        // Center a checkbox both horizontally and vertically in the current table cell
        private static bool Table_CenterCheckbox(string id, ref bool value)
        {
            var cursorPos = ImGui.GetCursorPos();
            var availWidth = ImGui.GetContentRegionAvail().X;

            var checkboxSize = ImGui.GetFrameHeight();
            ImGui.SetCursorPosX(cursorPos.X + (availWidth - checkboxSize) * 0.5f);
            ImGui.AlignTextToFramePadding();

            return ImGui.Checkbox($"##{id}", ref value);
        }
        private static void Table_VertCenterText(string text)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(text);
        }
        private static void Table_FullCenterText(string text)
        {
            var cursorPosX = ImGui.GetCursorPosX();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX(cursorPosX + (availWidth - textWidth) * 0.5f);
            ImGui.AlignTextToFramePadding();

            ImGui.TextUnformatted(text);
        }
        private static void Table_FullCenterText(string icon, Vector4 color)
        {
            var cursorPosX = ImGui.GetCursorPosX();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var textWidth = ImGui.CalcTextSize(icon).X;

            ImGui.SetCursorPosX(cursorPosX + (availWidth - textWidth) * 0.5f);
            ImGui.AlignTextToFramePadding();

            FontAwesome.Print(color, icon);
        }
        private static bool Table_CenteredButton(string label, Vector2? buttonSize = null)
        {
            var cursorPosX = ImGui.GetCursorPosX();
            var availWidth = ImGui.GetContentRegionAvail().X;

            Vector2 actualButtonSize;
            if (buttonSize.HasValue)
            {
                actualButtonSize = buttonSize.Value;
            }
            else
            {
                var textSize = ImGui.CalcTextSize(label);
                var framePadding = ImGui.GetStyle().FramePadding;
                actualButtonSize = new Vector2(textSize.X + framePadding.X * 2 + 10f, textSize.Y + framePadding.Y * 2);
            }

            ImGui.SetCursorPosX(cursorPosX + (availWidth - actualButtonSize.X) * 0.5f);
            return ImGui.Button(label, actualButtonSize);
        }
        private static void Table_FontCenter(FontAwesomeIcon icon)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
        }

        public static List<uint> GetOnlyPreviousMissionsRecursive(uint missionId)
        {
            if (!CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var missionInfo) || missionInfo.PreviousMissions.Contains(0))
                return [];

            var chain = GetOnlyPreviousMissionsRecursive(missionInfo.PreviousMissions.First());
            chain.Add(missionInfo.PreviousMissions.First());
            return chain;
        }
        private static List<uint> GetOnlyNextMissionsRecursive(uint missionId)
        {
            uint? nextMissionId = CosmicHelper.SheetMissionDict
                .Where(m => m.Value.PreviousMissions.First() == missionId)
                .Select(m => (uint?)m.Key)
                .FirstOrDefault();

            if (!nextMissionId.HasValue)
                return [];

            var chain = new List<uint> { nextMissionId.Value };
            chain.AddRange(GetOnlyNextMissionsRecursive(nextMissionId.Value));
            return chain;
        }
        private static unsafe void CompletionStatus_Formatted(uint id)
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return;

            var manager = (WKSManagerCustom*)managerPtr;
            var isCompleted = manager->IsMissionCompleted(id);
            var isGold = manager->IsMissionGolded(id);

            float availableWidth = ImGui.GetContentRegionAvail().X;

            if (isCompleted)
            {
                if (isGold)
                {
                    // Center the image
                    float imageWidth = 23f;
                    float offsetX = (availableWidth - imageWidth) * 0.5f;

                    if (offsetX > 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

                    if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                    {
                        if (tex.TryGetWrap(out var wrap, out var exc))
                        {
                            var cursorPos = ImGui.GetCursorPos();
                            var availWidth = ImGui.GetContentRegionAvail().X;
                            var availHeight = ImGui.GetFrameHeight();

                            // Center horizontally
                            ImGui.SetCursorPosX(cursorPos.X + (availWidth - 23) * 0.5f);

                            // Center vertically
                            ImGui.SetCursorPosY(cursorPos.Y + (availHeight - 23) * 0.5f);
                            ImGui.Image(wrap.Handle, new Vector2(23, 23), new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                        }
                    }
                }
                else
                {
                    ImGui.AlignTextToFramePadding();
                    Table_FullCenterText(FontAwesome.Check, EColor.Green);
                }
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                Table_FullCenterText(FontAwesome.Cross, EColor.Red);
            }
        }
        private static unsafe void CompletionStatus_Normal(uint id)
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return;

            var manager = (WKSManagerCustom*)managerPtr;
            var isCompleted = manager->IsMissionCompleted(id);
            var isGold = manager->IsMissionGolded(id);

            var containerSize = new Vector2(23, 23);

            // Create a consistent container for all elements
            var cursorPos = ImGui.GetCursorPos();
            ImGui.InvisibleButton("##status_container", containerSize);
            ImGui.SetCursorPos(cursorPos);

            if (isCompleted)
            {
                if (isGold)
                {
                    if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                    {
                        if (tex.TryGetWrap(out var wrap, out var exc))
                        {
                            ImGui.Image(wrap.Handle, containerSize, new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                        }
                    }
                }
                else
                {
                    // Center the FontAwesome icon within the container
                    var textSize = ImGui.CalcTextSize(FontAwesome.Check);
                    var offset = (containerSize - textSize) * 0.5f;
                    offset += new Vector2(-2f, 1f);
                    ImGui.SetCursorPos(cursorPos + offset);
                    FontAwesome.Print(EColor.Green, FontAwesome.Check);
                }
            }
            else
            {
                var textSize = ImGui.CalcTextSize(FontAwesome.Cross);
                var offset = (containerSize - textSize) * 0.5f;
                offset += new Vector2(-2f, 1f);
                ImGui.SetCursorPos(cursorPos + offset);
                FontAwesome.Print(EColor.Red, FontAwesome.Cross);
            }

            // Reset cursor to after the container
            ImGui.SetCursorPos(cursorPos + new Vector2(containerSize.X, 0));
        }
        private static void UpdateSelectedMission(uint missionId)
        {
            if (ImGui.IsItemClicked())
            {
                selectedMission = missionId;
            }
        }
        private static void DrawColoredStar(TurninState state)
        {
            Vector4 color = state switch
            {
                TurninState.Bronze => new Vector4(0.8f, 0.5f, 0.3f, 1.0f),  // Bronze
                TurninState.Silver => new Vector4(0.75f, 0.75f, 0.75f, 1.0f), // Silver
                TurninState.Gold => new Vector4(1.0f, 0.84f, 0.0f, 1.0f),    // Gold
                TurninState.Critical => new Vector4(1.0f, 0.84f, 0.0f, 1.0f), // Gold
                _ => new Vector4(0, 0, 0, 0) // Transparent/none
            };

            if (state != TurninState.None)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.PushFont(UiBuilder.IconFont); // Make sure you're using the icon font
                ImGui.Text(FontAwesomeIcon.Star.ToIconString());
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }
        }

        #endregion
    }
}
