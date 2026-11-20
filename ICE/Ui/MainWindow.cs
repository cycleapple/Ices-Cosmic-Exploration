using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using ICE.Ui.MainUi;
using ICE.Ui.MainUi.HelpFolder;
using ICE.Ui.MainUi.ModeSelect_Modes;
using ICE.Ui.MainUi.Settings;
using ICE.Ui.MainUi.Settings.Settings_Table;
using System.Collections.Generic;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace ICE.Ui
{
    internal class MainWindow : Window
    {
        public MainWindow() :
#if DEBUG
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} [Debug Build] ###ICEMainWindow2")
#else
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow2")
#endif
        {
            Flags = ImGuiWindowFlags.NoScrollbar;
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(500, 500),
                MaximumSize = new Vector2(4000, 4000),
            };
            TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("♥ Ko-fi (Buy me an ice coffee)"), Icon = FontAwesomeIcon.Heart, IconOffset = new(1, 1), Click = _ => GenericHelpers.ShellStart("https://ko-fi.com/ice643269") });

            P.windowSystem.AddWindow(this);

            AllowPinning = true;
            AllowClickthrough = true;
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 10).Push(ImGuiStyleVar.ChildBorderSize, 1);

            SelectableSidebar.Draw();

            ImGui.SameLine(0, 5);

            var windowSizeRemaining = ImGui.GetContentRegionAvail();
            using (var mainBody = ImRaii.Child("mainBody_WindowV3", windowSizeRemaining, true))
            {
                if (!mainBody.Success) return;
                DrawLegacyImport();
                if (LegacyImportError.Length != 0)
                {
                    ImGui.TextWrapped("舊設定未匯入：" + LegacyImportError);
                    ImGui.TextWrapped("原始設定檔未變更。目前使用獨立的新設定，開始執行前請選擇是否略過匯入。");
                    if (ImGui.Button("略過匯入，使用獨立新設定"))
                    {
                        LegacyImportError = "";
                        C.LegacyImportFailure = "";
                        C.Save();
                    }
                    ImGui.Separator();
                }
                if (C.LegacyImportWarnings.Length > 0 && ImGui.CollapsingHeader("舊設定匯入報告"))
                    foreach (var warning in C.LegacyImportWarnings) ImGui.TextWrapped(warning);
                MainBody();
            }
        }

        private static LegacyConfigImporter.ImportResult? importPreview;
        private static string importHash = "";
        private static string importMessage = "";
        private static void DrawLegacyImport()
        {
            var source = System.IO.Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "Mission Config.yaml");
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle))
            {
                if (ImGui.Button("預覽匯入舊版設定"))
                {
                    try
                    {
                        importHash = LegacyConfigImporter.SourceHash(source);
                        importPreview = LegacyConfigImporter.ConvertYaml(System.IO.File.ReadAllText(source));
                        importMessage = "";
                        ImGui.OpenPopup("匯入舊版設定");
                    }
                    catch (Exception error) { importMessage = "無法預覽：" + error.Message; }
                }
            }
            if (importMessage.Length > 0) ImGui.TextWrapped(importMessage);
            if (ImGui.BeginPopup("匯入舊版設定"))
            {
                ImGui.TextWrapped("將匯入任務、採集設定、配方與宇宙計畫。套用前會備份目前候選版設定，原始 YAML 不會變更。");
                if (importPreview != null)
                    foreach (var warning in importPreview.Warnings) ImGui.TextWrapped(warning);
                using (ImRaii.Disabled(importPreview == null || SchedulerMain.State != IceState.Idle))
                {
                    if (ImGui.Button("備份目前設定並套用匯入"))
                    {
                        try
                        {
                            var result = LegacyConfigImporter.ReplaceImport(source, ECommons.Configuration.EzConfig.DefaultConfigurationFileName, importHash);
                            P.ApplyImportedSettings(result.Json);
                            importMessage = "已匯入舊設定，請檢查任務與宇宙計畫後再開始。";
                            ImGui.CloseCurrentPopup();
                        }
                        catch (Exception error) { importMessage = "匯入失敗：" + error.Message; }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("取消")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            ImGui.Separator();
        }

        private static readonly Dictionary<WindowSelection, Action> SelectedView = new()
        {
            // Cosmic Helper
            [WindowSelection.MissionSetup] = () =>  Mission_Setup.Draw(),
            [WindowSelection.CosmicAgenda] = () => Cosmic_Agenda.Draw(),
            [WindowSelection.ExpeditionLogs] = () => Expedition_Log.Draw(),

            // Settings
            [WindowSelection.StopWhen] = () => StopWhen.Draw(),
            [WindowSelection.GatheringProfiles] = () => GatherSettings.Draw(),
            [WindowSelection.MissionPriority] = () => Priority_Settings.Draw(),
            [WindowSelection.CharacterSettings] = () => Character_Settings.Draw(),
            [WindowSelection.MiscSettings] = () => Misc_Settings.Draw(),
            [WindowSelection.TravelSettings] = () => TravelSettings.Draw(),

            // Hub Activities
            [WindowSelection.CreditShopping] = () => Shop_Credits.Draw(),
            [WindowSelection.GambaShopping] = () => GambaWheel.Draw(),
            [WindowSelection.DroneShopping] = () => Shop_Dronebit.Draw(),
            [WindowSelection.MountShopping] = () => Shop_Tokens.Draw(),

            // Help Section
            [WindowSelection.Plugin_Install] = () => helpSelect_Required.Draw(),
            [WindowSelection.Plugin_Logs] = () => helpSelect_Logs.Draw_Helper(),
            [WindowSelection.Plugin_Tips] = () => helpSelect_Tips.Draw(),
        };

        private static void MainBody()
        {
            var selectedWindow = C.SelectedTab;

            if (SelectedView.TryGetValue(selectedWindow, out var drawAction))
            {
                drawAction();
            }
            else
            {
                ImGui.Text("Hehe");
            }
        }

        public static void ModeSelection()
        {
            bool standard = C.SelectedMode == ModeSelect.Standard;
            bool relicMode = C.SelectedMode == ModeSelect.RelicMode;
            bool xpLeveling = C.SelectedMode == ModeSelect.LevelMode;
            bool goldMode = C.SelectedMode == ModeSelect.MissionGoldMode;
            bool agendaMode = C.SelectedMode == ModeSelect.AgendaMode;

            ImGui.Text("選擇模式");
            ImGui.Separator();

            if (ImGui.RadioButton("標準", standard))
            {
                C.SelectedMode = ModeSelect.Standard;
                C.Save();
            }
            ImGuiEx.HelpMarker(HelpInfoText(ModeSelect.Standard));
            if (ImGui.RadioButton("宇宙工具培養", relicMode))
            {
                C.SelectedMode = ModeSelect.RelicMode;
                C.Save();
            }
            ImGuiEx.HelpMarker(HelpInfoText(ModeSelect.RelicMode));

            if (ImGui.RadioButton("職業練等", xpLeveling))
            {
                C.SelectedMode = ModeSelect.LevelMode;
                C.Save();
            }
            ImGuiEx.HelpMarker(HelpInfoText(ModeSelect.LevelMode));
            if (ImGui.RadioButton("任務金牌收集", goldMode))
            {
                C.SelectedMode = ModeSelect.MissionGoldMode;
                C.Save();
            }
            ImGuiEx.HelpMarker(HelpInfoText(ModeSelect.MissionGoldMode));
            if (ImGui.RadioButton("宇宙計畫", agendaMode))
            {
                C.SelectedMode = ModeSelect.AgendaMode;
                C.Save();
            }
            ImGuiEx.HelpMarker(HelpInfoText(ModeSelect.AgendaMode));
        }
        public static string HelpInfoText(ModeSelect mode)
        {
            return mode switch
            {
                ModeSelect.Standard => "標準模式\n選擇要反覆執行的任務。預設依緊急任務、臨時任務、標準任務與工具精通任務排序。\n可在「設定 → 任務優先順序」調整。",
                ModeSelect.LevelMode => "職業練等\n依目前職業與等級選擇建議任務，執行期間會暫時套用對應設定。",
                ModeSelect.RelicMode => "宇宙工具培養\n依工具下一階段所需的研究資料選擇任務，也可限制為僅執行已啟用的任務。",
                ModeSelect.AgendaMode => "宇宙計畫\n依清單順序完成各職業的工具培養、等級、分數或信用點數目標；全部完成後停止。",
                ModeSelect.MissionGoldMode => "任務金牌收集\n選擇尚未取得金牌的任務，持續執行直到符合完成條件。",
                _ => "選擇執行模式。"
            };
        }
    }
}
