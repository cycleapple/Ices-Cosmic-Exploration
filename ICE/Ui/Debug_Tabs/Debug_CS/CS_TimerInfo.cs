using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Ui.Debug_Tabs.Debug_CS
{
    internal class CS_TimerInfo
    {
        public static void Draw()
        {
            ImGui.Text("All world timers:");
            TimerUpdate();

            if (CosmicHandler.EventInfo() is { } info)
            {
                ImGui.Text($"State: {info.wksEvent}");
                ImGui.Text($"Timer: {info.timer:N0}");
            }
        }

        private static unsafe void TimerUpdate()
        {
            var c = UIState.Instance()->MassivePcContentTodo.Director;
            if (c != null)
            {
                for (int i = 0; i < c->Objectives.Count; i++)
                {
                    var objective = c->Objectives[i];
                    if (objective.Enabled)
                        ImGuiEx.Text($"Objective {i}: raw timer {objective.TimeLeft}");
                }
            }
        }
    }
}
