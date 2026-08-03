using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using ICE.Config;
using ICE.Utilities.ImGuiTools;
using Lumina.Excel.Sheets;

namespace ICE.Ui.MainUi.ModeSelect
{
    internal static class modeSelect_Agenda
    {
        private static uint _newJob = 8;
        private static AgendaGoal _newGoal = AgendaGoal.RelicStage;

        public static void Draw()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.ClipboardList, "宇宙計畫模式");
            ImGui.TextWrapped("依序完成目標；每次任務結束後會重新檢查，並自動切換到第一個尚未完成目標的職業。計畫模式會忽略「停止條件」頁面的停止設定。");
            ImGui.TextWrapped("提示：目前目標為宇宙工具階段且經驗已滿時，會強制提交並升級，無論是否勾選「宇宙工具完成時繳交」。");
            ImGui.Separator();

            bool enabled = C.CosmicAgendaMode;
            if (ImGui.Checkbox("啟用宇宙計畫模式", ref enabled))
            {
                C.CosmicAgendaMode = enabled;
                if (enabled)
                {
                    C.XPRelicGrind = false;
                    C.GrindProvisionals = false;
                }
                C.Save();
            }

            ImGui.SameLine();
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || C.CosmicAgenda.Count == 0 || !C.CosmicAgendaMode))
            {
                if (ImGui.Button("開始", new Vector2(120, 0)))
                {
                    SchedulerMain.EnablePlugin();
                }
            }
            ImGui.SameLine();
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("停止", new Vector2(120, 0)))
                {
                    SchedulerMain.DisablePlugin();
                }
            }

            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle))
            {
                ImGui.Separator();
                DrawAddEntry();
                DrawEntries();
            }
        }

        private static void DrawAddEntry()
        {
            ImGui.Text("新增目標");
            ImGui.SetNextItemWidth(140);
            if (ImGui.BeginCombo("職業", JobName(_newJob)))
            {
                for (uint job = 8; job <= 18; job++)
                {
                    if (ImGui.Selectable(JobName(job), _newJob == job))
                        _newJob = job;
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(180);
            if (ImGui.BeginCombo("目標", GoalName(_newGoal)))
            {
                foreach (AgendaGoal goal in Enum.GetValues<AgendaGoal>())
                {
                    if (ImGui.Selectable(GoalName(goal), _newGoal == goal))
                    {
                        _newGoal = goal;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("加入計畫"))
            {
                C.CosmicAgenda.Add(new AgendaEntry
                {
                    Job = _newJob,
                    Goal = _newGoal,
                    Target = DefaultTarget(_newGoal),
                    StandardGoldARank = C.StopStandardGoldARank,
                    StandardGoldBRank = C.StopStandardGoldBRank,
                    StandardGoldCRank = C.StopStandardGoldCRank,
                    StandardGoldDRank = C.StopStandardGoldDRank,
                });
                C.Save();
            }
        }

        private static void DrawEntries()
        {
            if (C.CosmicAgenda.Count == 0)
            {
                ImGui.TextDisabled("尚未加入目標。");
                return;
            }

            bool changed = false;
            int? remove = null;
            if (ImGui.BeginTable("CosmicAgenda", 6, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("順序", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("職業", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("目標", ImGuiTableColumnFlags.WidthFixed, 130);
                ImGui.TableSetupColumn("設定", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("警告", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableHeadersRow();

                for (var index = 0; index < C.CosmicAgenda.Count; index++)
                {
                    var entry = C.CosmicAgenda[index];
                    ImGui.PushID(index);
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text((index + 1).ToString());
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(JobName(entry.Job));
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(GoalName(entry.Goal));
                    ImGui.TableSetColumnIndex(3);
                    var aRank = true;
                    var bRank = true;
                    var cRank = true;
                    var dRank = true;

                    if (entry.Goal == AgendaGoal.StandardMissionsGolded)
                    {
                        aRank = entry.StandardGoldARank ?? C.StopStandardGoldARank;
                        bRank = entry.StandardGoldBRank ?? C.StopStandardGoldBRank;
                        cRank = entry.StandardGoldCRank ?? C.StopStandardGoldCRank;
                        dRank = entry.StandardGoldDRank ?? C.StopStandardGoldDRank;

                        ImGui.TextDisabled("納入：");
                        ImGui.SameLine();
                        if (ImGui.Checkbox("A", ref aRank)) { entry.StandardGoldARank = aRank; changed = true; }
                        ImGui.SameLine();
                        if (ImGui.Checkbox("B", ref bRank)) { entry.StandardGoldBRank = bRank; changed = true; }
                        ImGui.SameLine();
                        if (ImGui.Checkbox("C", ref cRank)) { entry.StandardGoldCRank = cRank; changed = true; }
                        ImGui.SameLine();
                        if (ImGui.Checkbox("D", ref dRank)) { entry.StandardGoldDRank = dRank; changed = true; }
                    }
                    else
                    {
                        ImGui.SetNextItemWidth(100);
                        var target = entry.Target;
                        if (ImGui.InputInt("目標值", ref target))
                        {
                            entry.Target = Math.Max(1, target);
                            changed = true;
                        }
                    }

                    ImGui.TableSetColumnIndex(4);
                    using (ImRaii.Disabled(index == 0))
                    {
                        if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, "上移"))
                        {
                            (C.CosmicAgenda[index - 1], C.CosmicAgenda[index]) = (C.CosmicAgenda[index], C.CosmicAgenda[index - 1]);
                            changed = true;
                        }
                    }
                    ImGui.SameLine();
                    using (ImRaii.Disabled(index == C.CosmicAgenda.Count - 1))
                    {
                        if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, "下移"))
                        {
                            (C.CosmicAgenda[index + 1], C.CosmicAgenda[index]) = (C.CosmicAgenda[index], C.CosmicAgenda[index + 1]);
                            changed = true;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, "刪除"))
                    {
                        remove = index;
                    }
                    ImGui.TableSetColumnIndex(5);
                    var warning = WarningText(entry, aRank, bRank, cRank, dRank);
                    if (warning != null)
                        ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), warning);
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }

            if (remove.HasValue)
            {
                C.CosmicAgenda.RemoveAt(remove.Value);
                changed = true;
            }
            if (changed)
            {
                C.Save();
            }
        }

        private static string? WarningText(AgendaEntry entry, bool aRank = true, bool bRank = true, bool cRank = true, bool dRank = true) => entry.Goal switch
        {
            AgendaGoal.StandardMissionsGolded when !aRank && !bRank && !cRank && !dRank => "請選階級",
            AgendaGoal.RelicStage when entry.Target > CosmicHelper.DefaultRelicStageTarget => $"階段上限 {CosmicHelper.DefaultRelicStageTarget}",
            AgendaGoal.CosmoCredits when entry.Target > 30_000 => "宇宙信用點上限 30,000",
            AgendaGoal.LunarCredits when entry.Target > 10_000 => "星球信用點上限 10,000",
            AgendaGoal.ClassLevel when entry.Target > 100 => "職業等級上限 100",
            AgendaGoal.ClassScore when entry.Target > 500_000 => "職業技巧點超過 500,000",
            _ => null,
        };

        private static string JobName(uint job) => Svc.Data.GetExcelSheet<ClassJob>().GetRow(job).Name.ToString();

        private static string GoalName(AgendaGoal goal) => goal switch
        {
            AgendaGoal.RelicStage => "宇宙工具階段",
            AgendaGoal.CosmoCredits => "宇宙信用點",
            AgendaGoal.ClassLevel => "職業等級",
            AgendaGoal.ClassScore => "職業分數",
            AgendaGoal.StandardMissionsGolded => "普通任務全金",
            AgendaGoal.LunarCredits => "星球信用點",
            _ => goal.ToString(),
        };

        private static int DefaultTarget(AgendaGoal goal) => goal switch
        {
            AgendaGoal.RelicStage => CosmicHelper.DefaultRelicStageTarget,
            AgendaGoal.CosmoCredits => 30_000,
            AgendaGoal.LunarCredits => 10_000,
            AgendaGoal.ClassLevel => 100,
            AgendaGoal.ClassScore => 500_000,
            _ => 1,
        };
    }
}
