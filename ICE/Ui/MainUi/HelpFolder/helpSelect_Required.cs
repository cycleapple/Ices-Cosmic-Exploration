using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.HelpFolder
{
    internal class helpSelect_Required
    {
        public static void Draw()
        {
            ImGui.TextWrapped("以下為本插件正常運作所需的插件。若未安裝，部分功能將無法使用。");

            ImGui.Separator();
            ImGuiEx.IconWithText(FontAwesomeIcon.Hammer, "製作");
            HasPlugin("https://love.puni.sh/ment.json", "Artisan");

            ImGui.Separator();
            ImGuiEx.IconWithText(FontAwesomeIcon.Feather, "採集");
            ImGui.Text("園藝工／採礦工／捕魚人適用");
            HasPlugin("https://puni.sh/api/repository/veyn", "vnavmesh");
            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Text("僅限捕魚人");
            HasPlugin("https://love.puni.sh/ment.json", "AutoHook");

            ImGui.Separator();
            ImGuiEx.IconWithText(FontAwesomeIcon.Running, "自動執行據點活動");
            HasPlugin("https://puni.sh/api/repository/veyn", "vnavmesh");
        }

        public static void HasPlugin(string repo, string pluginName)
        {
            bool isInstalled = DalamudReflector.HasRepo($"{repo}");
            if (isInstalled)
            {
                FontAwesome.Print(EColor.Green, FontAwesome.Check);
                ImGui.SameLine();
                ImGui.Text($"已安裝 {pluginName} 軟體庫");
            }
            else
            {
                FontAwesome.Print(EColor.Red, FontAwesome.Cross);
                ImGui.SameLine();
                if (ImGui.Button($"安裝 {pluginName} 軟體庫"))
                {
                    DalamudReflector.AddRepo(repo, true);
                    DalamudReflector.SaveDalamudConfig();
                }
            }

            bool hasPlugin = Utils.HasPlugin($"{pluginName}");

            if (hasPlugin)
            {
                FontAwesome.Print(EColor.Green, FontAwesome.Check);
                ImGui.SameLine();
                ImGui.Text($"已安裝 {pluginName}");
            }
            else
            {
                FontAwesome.Print(EColor.Red, FontAwesome.Cross);
                ImGui.SameLine();
                using (ImRaii.Disabled(installingPlugin))
                {
                    if (ImGui.Button($"安裝 {pluginName}"))
                    {
                        _ = InstallPlugin(repo, pluginName);
                    }
                }
            }
        }

        private static bool installingPlugin = false;
        private static async Task InstallPlugin(string repo, string pluginName)
        {
            if (installingPlugin) return; // Already installing

            installingPlugin = true;
            try
            {
                await DalamudReflector.AddPlugin(repo, pluginName);
                DalamudReflector.SaveDalamudConfig();
            }
            finally
            {
                installingPlugin = false;
            }
        }
    }
}
