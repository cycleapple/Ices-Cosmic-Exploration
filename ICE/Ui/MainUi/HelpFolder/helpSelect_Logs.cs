using Dalamud.Interface.Utility.Raii;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ICE.Utilities.Cosmic_Helper.IceLogging;

namespace ICE.Ui.MainUi.HelpFolder
{
    internal class helpSelect_Logs
    {
        private static string searchFilter = string.Empty;

        public static void Draw_Helper()
        {
            using (var headerChild = ImRaii.Child("##helpSelect_Logs", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!headerChild.Success) return; // Ensures that it was loaded properly before continuing.
                if (ImGui.BeginTabBar("Ice Log Tabs"))
                {
                    if (ImGui.BeginTabItem("主要紀錄"))
                    {
                        LogHelperViewer();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("目的地紀錄"))
                    {
                        DestinationLogViewer();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
        }

        public static void Draw_Debug()
        {
            if (ImGui.Button("複製紀錄到剪貼簿"))
            {
                LogSystem.CopyToClipboard();
            }
            LogHelperViewer();
        }

        private static void LogHelperViewer()
        {
            // Search input
            ImGui.SetNextItemWidth(300);
            ImGui.InputTextWithHint("##LogSearch", "搜尋紀錄…", ref searchFilter, 256);

            ImGui.SameLine();
            if (ImGui.Button("清除"))
            {
                searchFilter = string.Empty;
            }

            ImGui.Spacing();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("LogTable", 4, flags))
            {
                ImGui.TableSetupColumn("時間");
                ImGui.TableSetupColumn("等級");
                ImGui.TableSetupColumn("分類");
                ImGui.TableSetupColumn("訊息");
                ImGui.TableHeadersRow();

                // Filter logs based on search input
                var filteredLogs = LogSystem.Logs.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(searchFilter))
                {
                    filteredLogs = filteredLogs.Where(log =>
                        log.Message.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                        (log.Category?.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        log.Level.ToString().Contains(searchFilter, StringComparison.OrdinalIgnoreCase)
                    );
                }

                foreach (var log in filteredLogs.OrderByDescending(l => l.Timestamp))
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(log.Timestamp.ToString("HH:mm:ss"));

                    ImGui.TableNextColumn();
                    // Color-code by level
                    var color = log.Level switch
                    {
                        LogLevel.Error => new Vector4(1, 0, 0, 1),
                        LogLevel.Warning => new Vector4(1, 1, 0, 1),
                        LogLevel.Info => new Vector4(0, 1, 1, 1),
                        _ => new Vector4(0.7f, 0.7f, 0.7f, 1)
                    };
                    ImGui.TextColored(color, log.Level.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(log.Category ?? "");

                    ImGui.TableNextColumn();
                    ImGui.Text(log.Message);
                }

                ImGui.EndTable();
            }
        }

        private static void DestinationLogViewer()
        {
            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.Borders |
                                    ImGuiTableFlags.ScrollY |
                                    ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("Destination Log Viewer", 5, flags))
            {
                ImGui.TableSetupColumn("時間");
                ImGui.TableSetupColumn("起點");
                ImGui.TableSetupColumn("目的地");
                ImGui.TableSetupColumn("距離");

                ImGui.TableHeadersRow();

                var filteredLogs = DestinationLogs.Logs.AsEnumerable();
                var entryNumber = 0;

                foreach (var log in filteredLogs.OrderByDescending(l => l.Timestamp))
                {
                    ImGui.TableNextRow();

                    ImGui.PushID($"{log.PlayerDestination}_{entryNumber}");

                    ImGui.TableSetColumnIndex(0);
                    Table_VertCenterText(log.Timestamp.ToString("HH:mm:ss"));

                    ImGui.TableNextColumn();
                    Table_VertCenterText($"X: {log.PlayerStart.X:N2}, Y: {log.PlayerStart.Y:N2}, Z: {log.PlayerStart.Z:N2}");

                    ImGui.TableNextColumn();
                    Table_VertCenterText($"X: {log.PlayerDestination.X:N2}, Y: {log.PlayerDestination.Y:N2}, Z: {log.PlayerDestination.Z:N2}");

                    ImGui.TableNextColumn();
                    Table_VertCenterText($"{log.Distance}");

                    ImGui.TableNextColumn();
                    if (ImGui.Button("複製資訊"))
                    {
                        var clipboardText = new StringBuilder();
                        clipboardText.AppendLine($"Start: X: {log.PlayerStart.X:N2}, Y: {log.PlayerStart.Y:N2}, Z: {log.PlayerStart.Z:N2}");
                        clipboardText.Append($"End: X: {log.PlayerDestination.X:N2}, Y: {log.PlayerDestination.Y:N2}, Z: {log.PlayerDestination.Z:N2}");
                        ImGui.SetClipboardText($"{clipboardText}");
                        Notify.Success("已將紀錄複製到剪貼簿");
                    }
                    ImGui.PopID();

                    entryNumber += 1;
                }

                ImGui.EndTable();
            }
        }
        private static void Table_VertCenterText(string text)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(text);
        }
    }
}
