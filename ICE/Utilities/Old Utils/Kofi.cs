using Dalamud.Interface.Utility;
using ECommons.EzSharedDataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Utilities;

internal class Kofi // Heavily borrowed from the Ecommons version. 
{
    public static void DrawRight()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(cur.X + ImGui.GetContentRegionAvail().X - ImGuiHelpers.GetButtonSize(Text).X);
        DrawRaw();
        ImGui.SetCursorPos(cur);
    }

    public static void DrawRaw()
    {
        DrawButton();
    }

    private static uint ColorNormal
    {
        get
        {
            var vector1 = ImGuiEx.Vector4FromRGB(0x022594);
            var vector2 = ImGuiEx.Vector4FromRGB(0x940238);

            var gen = GradientColor.Get(vector1, vector2).ToUint();
            var data = EzSharedData.GetOrCreate<uint[]>("ECommonsPatreonBannerRandomColor", [gen]);
            if (!GradientColor.IsColorInRange(data[0].ToVector4(), vector1, vector2))
            {
                data[0] = gen;
            }
            return data[0];
        }
    }

    public static string Text = "♥ Ko-fi";
    public static string DonateLink => "https://ko-fi.com/ice643269";

    private static uint ColorHovered => ColorNormal;

    private static uint ColorActive => ColorNormal;

    private static readonly uint ColorText = 0xFFFFFFFF;

    private static string PatreonButtonTooltip => $"""
				If you like {Svc.PluginInterface.Manifest.Name}, please consider supporting it's developer via Ko-Fi! 
				Help prevent against global warning by telling good dad jokes and keeping me warm.
				Left click - to go to Ko-Fi;
				""";

    public static void DrawButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ColorNormal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorActive);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorText);
        if (ImGui.Button(Text))
        {
            GenericHelpers.ShellStart(DonateLink);
        }
        Popup();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        ImGui.PopStyleColor(4);
    }

    private static void Popup()
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGuiEx.Text(PatreonButtonTooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
    }
}
