using ICE.Config;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table;

public static class Settings_TableColumns
{
    private static string[] missionSortOptions = ["ID", "名稱", "宇宙信用點", "星球信用點", "經驗值 I", "經驗值 II", "經驗值 III", "經驗值 IV", "經驗值 V", "地圖位置", "職業分數"];

    public static void ColumnSettings()
    {
        int missionSelectedOption = C.TableSortOption;
        if (ImGui.BeginCombo("排序方式", missionSortOptions[missionSelectedOption]))
        {
            for (int i = 0; i < missionSortOptions.Length; i++)
            {
                bool isSelected = (i == missionSelectedOption);
                if (ImGui.Selectable(missionSortOptions[i], isSelected))
                {
                    missionSelectedOption = i;
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
                if (missionSelectedOption != C.TableSortOption)
                {
                    C.TableSortOption = missionSelectedOption;
                    C.Save();
                }
            }
            ImGui.EndCombo();
        }

        bool hideUnsupported = C.HideUnsupportedMissions;
        if (ImGui.Checkbox("隱藏不支援的任務", ref hideUnsupported))
        {
            C.HideUnsupportedMissions = hideUnsupported;
            C.Save();
        }

        bool showExtraInfo = C.ShowExtraMissionInfo;
        if (ImGui.Checkbox("顯示額外任務資訊側欄", ref showExtraInfo))
        {
            C.ShowExtraMissionInfo = showExtraInfo;
            C.Save();
        }

        bool autoShowToken = C.Auto_ShowTokens;
        if (ImGui.Checkbox("自動隱藏／顯示星球代幣", ref autoShowToken))
        {
            C.Auto_ShowTokens = autoShowToken;
            C.Save();
        }



        bool showManualMode = C.ShowManualMode;
        if (ImGui.Checkbox("顯示手動模式欄", ref showManualMode))
        {
            C.ShowManualMode = showManualMode;
            if (!showManualMode)
            {
                foreach (var mission in C.MissionConfig)
                {
                    mission.Value.ManualMode = false;
                }
            }
            C.Save();
        }
        ImGuiEx.HelpMarker("只有打算自行執行任務、不使用自動化時才啟用；或由其他插件負責繳交、製作與採集，不讓 ICE 控制這些插件時使用。");
    }

    private static bool ApplyToAllClasses = true;
    private static bool ApplyToSpecicClass = false;
    private static int SpecificClass = 8;
    private static int selectedClassIndex = 0;

    private static readonly string[] classOptions = new[]
    {
        "刻木匠（CRP）",      // 0
        "鍛鐵匠（BSM）",     // 1
        "鑄甲匠（ARM）",        // 2
        "雕金匠（GSM）",       // 3
        "製革匠（LTW）",  // 4
        "裁衣匠（WVR）",         // 5
        "鍊金術士（ALC）",      // 6
        "烹調師（CUL）",     // 7
        "採礦工（MIN）",          // 8
        "園藝工（BTN）",       // 9
        "捕魚人（FSH）"          // 10
    };

    private static readonly int[] classIds = new[]
    {
        8,  // Carpenter
        9,  // Blacksmith
        10, // Armorer
        11, // Goldsmith
        12, // Leatherworker
        13, // Weaver
        14, // Alchemist
        15, // Culinarian
        16, // Miner
        17, // Botanist
        18  // Fisher
    };

    private static bool AnyTurnin = true;
    private static bool TurninGold = false;
    private static bool TurninSilver = false;
    private static bool TurninBronze = false;

    public static void GeneralMissionSettings()
    {
        bool onlyGrabMission = C.OnlyGrabMission;
        if (ImGui.Checkbox("只領取任務", ref onlyGrabMission))
        {
            C.OnlyGrabMission = onlyGrabMission;
            C.Save();
        }

        bool removeGold = C.RemoveAfterGold;
        if (ImGui.Checkbox("取得金牌後移除任務", ref removeGold))
        {
            C.RemoveAfterGold = removeGold;
            C.Save();
        }

        bool removeAfterThreeGoldFailures = C.RemoveAfterThreeGoldFailures;
        if (ImGui.Checkbox("連續三次未獲金牌後移除任務", ref removeAfterThreeGoldFailures))
        {
            C.RemoveAfterThreeGoldFailures = removeAfterThreeGoldFailures;
            C.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("?");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("僅計算以自動或金牌繳交完成的任務；設定為銀牌或銅牌繳交的任務不會被移除。取得金牌會重設計數。");

        bool skipHubDuringRedAlert = C.SkipHubActivitiesDuringRedAlert;
        if (ImGui.Checkbox("緊急任務期間跳過維護與 Hub 活動", ref skipHubDuringRedAlert))
        {
            C.SkipHubActivitiesDuringRedAlert = skipHubDuringRedAlert;
            C.Save();
        }

        ImGui.Checkbox("目前任務完成後停止", ref Mission_Settings.StopAfterCurrent);
        bool relicTurnin = C.TurninRelic;
        if (ImGui.Checkbox("宇宙工具完成時繳交##RelicTurnin_GeneralSetting", ref relicTurnin))
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
            ImGui.SetTooltip("以下說明此功能的運作方式：\n" +
                             "1：會檢查角色目前實際使用的職業，而非選單中選取的職業。\n" +
                             "2：若要全自動執行，不能裝備該宇宙工具。\n" +
                             "3：此功能的優先度高於「宇宙工具完成時停止」；兩者同時啟用時會繳交並繼續。\n" +
                             "4：若目前為製作職業，繳交後可返回原先進行製作的位置。");
        }
        if (ImGui.Button("快速套用繳交設定"))
        {
            ImGui.OpenPopup("Quick Apply_Mission Turnins");
        }

        if (ImGui.BeginPopup("Quick Apply_Mission Turnins"))
        {
            if (ImGui.RadioButton("套用至所有職業", ApplyToAllClasses))
            {
                ApplyToAllClasses = true;
                ApplyToSpecicClass = false;
            }

            if (ImGui.RadioButton("套用至指定職業", ApplyToSpecicClass))
            {
                ApplyToAllClasses = false;
                ApplyToSpecicClass = true;
            }
            if (ImGui.Combo("##ClassSelector", ref selectedClassIndex, classOptions, classOptions.Length))
            {
                // Update SpecificClass when selection changes
                SpecificClass = classIds[selectedClassIndex];
                IceLogging.Debug($"Selected class: {classOptions[selectedClassIndex]}, ID: {SpecificClass}");
            }
            ImGui.Separator();
            ImGui.Text("選擇繳交選項");
            ImGui.Dummy(new Vector2(0, 2));

            if (ImGui.Checkbox("自動", ref AnyTurnin))
            {
                if (AnyTurnin)
                {
                    TurninGold = false;
                    TurninSilver = false;
                    TurninBronze = false;

                    AnyTurnin = true;
                }
                else
                {
                    if (!(TurninBronze && TurninSilver && TurninGold))
                    {
                        AnyTurnin = true;
                    }
                }

                C.Save();
            }
            ImGuiEx.HelpMarker("此選項會盡量取得最佳成果，但必要時會繳交任何成果且不停止。");

            ImGui.Separator();

            if (ImGui.Checkbox("金牌", ref TurninGold))
            {
                if (AnyTurnin && TurninGold)
                    AnyTurnin = false;

            }
            if (ImGui.Checkbox("銀牌", ref TurninSilver))
            {
                if (AnyTurnin && TurninSilver)
                    AnyTurnin = false;

            }
            if (ImGui.Checkbox("銅牌", ref TurninBronze))
            {
                if (AnyTurnin && TurninBronze)
                    AnyTurnin = false;

            }

            if (!AnyTurnin && !TurninGold && !TurninSilver && !TurninBronze)
                AnyTurnin = true;

            ImGui.Separator();

            if (ImGui.Button("套用"))
            {
                var amountApplied = 0;
                foreach (var mission in C.MissionConfig)
                {
                    if (CosmicHelper.SheetMissionDict.TryGetValue(mission.Key, out var sheetInfo))
                    {
                        if (ApplyToSpecicClass && !sheetInfo.Jobs.Contains((uint)SpecificClass))
                            continue;

                        if (sheetInfo.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining))
                            continue;

                        if (C.MissionConfig.TryGetValue(mission.Key, out var config))
                        {
                            config.AutoTurnin = AnyTurnin;
                            config.TurninGold = TurninGold;
                            config.TurninSilver = TurninSilver;
                            config.TurninBronze = TurninBronze;
                        }
                        amountApplied += 1;
                    }
                }
                C.SaveDebounced();

                Notify.Success($"已將設定套用至 {amountApplied} 個任務。");
                ImGui.CloseCurrentPopup();
            }


            ImGui.EndPopup();
        }
    }
}
