using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class SafetySettings
    {
        private static bool delayGrabMission = C.DelayGrabMission;
        private static int delayAmount = C.DelayIncrease;
        private static bool delayCraft = C.DelayCraft;
        private static int delayCraftAmount = C.DelayCraftIncrease;

        public static void Draw()
        {
            if (ImGui.Checkbox("在任務選單加入延遲", ref delayGrabMission))
            {
                C.DelayGrabMission = delayGrabMission;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "這項延遲用於提高安全性。若想縮短任務之間的等待時間，可自行降低。\n" +
                "建議值約為 250 ms；若遇到動畫鎖定，可提高此值。");
            if (delayGrabMission)
            {
                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();
                if (ImGui.SliderInt("ms###Mission", ref delayAmount, 0, 1000))
                {
                    if (C.DelayIncrease != delayAmount)
                    {
                        C.DelayIncrease = delayAmount;
                        C.SaveDebounced();
                    }
                }
            }
            if (ImGui.Checkbox("在製作選單加入延遲", ref delayCraft))
            {
                C.DelayCraft = delayCraft;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "這項延遲用於提高安全性。若想縮短繳交前的等待時間，可自行降低。\n" +
                "建議值約為 2500 ms；若遇到動畫鎖定，可提高此值。");
            if (delayCraft)
            {
                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();
                if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 500, 5000))
                {
                    if (C.DelayCraftIncrease != delayCraftAmount)
                    {
                        C.DelayCraftIncrease = delayCraftAmount;
                        C.SaveDebounced();
                    }
                }
            }
            bool jumpIfStuck = C.JumpIfStuck;
            if (ImGui.Checkbox("導航移動卡住時跳躍", ref jumpIfStuck))
            {
                C.JumpIfStuck = jumpIfStuck;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "使用 navmesh 移動卡住時，經過一段時間（目前為 3 秒）會嘗試跳躍。\n" +
                "注意：這是實驗性功能。若在特定位置卡住，請透過紀錄功能回報位置與相關資訊。");
        }
    }
}
