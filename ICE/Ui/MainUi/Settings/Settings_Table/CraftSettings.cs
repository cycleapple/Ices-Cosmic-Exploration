using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ICE.Config;
using ICE.Utilities.Cosmic_Helper;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ICE.Ui.MainUi.Settings.Settings_Table;

internal static class CraftSettings
{
    private static string newProfileName = "";
    private static string[] availableSolvers = [];
    private static uint[] availableFoodNq = [];
    private static uint[] availableFoodHq = [];
    private static uint[] availablePotsNq = [];
    private static uint[] availablePotsHq = [];
    private static bool scanned;
    private static int applyMissionType;
    private static int applyProgress;
    private static int applyQuality;

    public static void Draw()
    {
        EnsureProfiles();
        ImGuiEx.IconWithText(FontAwesomeIcon.Hammer, "Artisan 製作設定檔");
        ImGui.TextWrapped("設定只在 ICE 執行指定任務期間暫時套用；任務結束、放棄、停止 ICE 或卸載插件時都會還原 Artisan 原設定。");

        if (!P.Artisan.Installed)
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), "Artisan 未安裝或未載入；可以編輯設定，但執行已套用設定檔的任務時 ICE 會停止。 ");

        ImGui.SetNextItemWidth(220);
        ImGui.InputText("新設定檔名稱", ref newProfileName, 64);
        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(newProfileName)))
        {
            if (ImGui.Button("新增設定檔"))
            {
                var id = C.CraftProfiles.Keys.DefaultIfEmpty(0).Max() + 1;
                C.CraftProfiles[id] = new CraftProfile { Id = id, Name = newProfileName.Trim() };
                C.SelectedCraftProfileId = id;
                newProfileName = "";
                C.Save();
            }
        }

        if (!C.CraftProfiles.TryGetValue(C.SelectedCraftProfileId, out var profile))
        {
            C.SelectedCraftProfileId = 0;
            profile = C.CraftProfiles[0];
        }

        if (ImGui.BeginTable("CraftProfiles", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("設定檔", ImGuiTableColumnFlags.WidthFixed, 230);
            ImGui.TableSetupColumn("設定", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            foreach (var (id, entry) in C.CraftProfiles.OrderBy(entry => entry.Key))
            {
                if (ImGui.Selectable($"{entry.Name}##craft-profile-{id}", C.SelectedCraftProfileId == id))
                {
                    C.SelectedCraftProfileId = id;
                    profile = entry;
                    C.Save();
                }
            }

            using (ImRaii.Disabled(profile.Id == 0))
            {
                if (ImGui.Button("刪除選取的設定檔"))
                {
                    var deletedId = profile.Id;
                    C.CraftProfiles.Remove(deletedId);
                    foreach (var mission in C.MissionConfig.Values)
                    {
                        foreach (var settings in mission.CraftSettings.Values)
                        {
                            if (settings.CraftProfileId == deletedId)
                                settings.CraftProfileId = -1;
                        }
                    }
                    C.SelectedCraftProfileId = 0;
                    profile = C.CraftProfiles[0];
                    C.Save();
                }
            }

            ImGui.TableSetColumnIndex(1);
            var profileName = profile.Name;
            ImGui.SetNextItemWidth(260);
            if (ImGui.InputText("名稱", ref profileName, 64))
            {
                profile.Name = profileName;
                C.SaveDebounced();
            }

            if (!scanned && P.Artisan.Installed)
                RefreshAvailableOptions();
            if (ImGui.Button("重新掃描 Artisan 選項"))
                RefreshAvailableOptions();
            ImGui.SameLine();
            ImGui.TextDisabled(scanned ? $"{availableSolvers.Length} 個求解器" : "尚未掃描");
            DrawRecipeSettings(profile.Settings, $"profile-{profile.Id}", availableSolvers);

            ImGui.Separator();
            string[] missionTypes = ["所有製作任務", "一般製作", "高難製作"];
            ImGui.Combo("套用任務類型", ref applyMissionType, missionTypes, missionTypes.Length);
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt("指定進展（0 表示不限）", ref applyProgress);
            applyProgress = Math.Max(0, applyProgress);
            ImGui.SetNextItemWidth(130);
            ImGui.InputInt("指定品質（0 表示不限）", ref applyQuality);
            applyQuality = Math.Max(0, applyQuality);
            if (ImGui.Button("套用至符合條件的配方"))
            {
                var count = ApplyProfile(profile.Id, applyMissionType, applyProgress, applyQuality);
                IceLogging.Info($"製作設定檔「{profile.Name}」已套用至 {count} 個配方。", "[Craft Profiles]");
                C.Save();
            }

            ImGui.EndTable();
        }
    }

    public static void DrawRecipeAssignment(uint missionId, ushort recipeId)
    {
        EnsureProfiles();
        if (!C.MissionConfig.TryGetValue(missionId, out var missionConfig))
            return;

        missionConfig.CraftSettings.TryGetValue(recipeId, out var settings);
        var currentLabel = settings == null
            ? "Artisan 原設定"
            : settings.CraftProfileId >= 0 && C.CraftProfiles.TryGetValue(settings.CraftProfileId, out var profile)
                ? profile.Name
                : "自訂";

        ImGui.SetNextItemWidth(280);
        if (ImGui.BeginCombo($"ICE 製作設定##recipe-assignment-{missionId}-{recipeId}", currentLabel))
        {
            if (ImGui.Selectable("Artisan 原設定", settings == null))
            {
                missionConfig.CraftSettings.Remove(recipeId);
                settings = null;
                C.Save();
            }
            if (ImGui.Selectable("自訂", settings != null && settings.CraftProfileId < 0))
            {
                settings ??= new();
                settings.CraftProfileId = -1;
                missionConfig.CraftSettings[recipeId] = settings;
                C.Save();
            }
            foreach (var (profileId, entry) in C.CraftProfiles.OrderBy(entry => entry.Key))
            {
                if (ImGui.Selectable(entry.Name, settings?.CraftProfileId == profileId))
                {
                    settings ??= new();
                    settings.CraftProfileId = profileId;
                    missionConfig.CraftSettings[recipeId] = settings;
                    C.Save();
                }
            }
            ImGui.EndCombo();
        }

        if (settings is { CraftProfileId: < 0 })
            DrawRecipeSettings(settings, $"recipe-{missionId}-{recipeId}", P.Artisan.Installed ? P.Artisan.GetAvailableSolvers(recipeId) : []);
    }

    private static void DrawRecipeSettings(ArtisanRecipeSettings settings, string id, IEnumerable<string> solvers)
    {
        var changed = false;
        var solverName = settings.SolverName.Length == 0 ? "Artisan 原設定" : settings.SolverName;
        ImGui.SetNextItemWidth(280);
        if (ImGui.BeginCombo($"求解器##solver-{id}", solverName))
        {
            if (ImGui.Selectable("Artisan 原設定", settings.SolverName.Length == 0))
            {
                settings.SolverName = "";
                changed = true;
            }
            foreach (var solver in solvers.Distinct().Order())
            {
                if (ImGui.Selectable(solver, settings.SolverName == solver))
                {
                    settings.SolverName = solver;
                    changed = true;
                }
            }
            ImGui.EndCombo();
        }

        changed |= DrawConsumable("食物", $"food-{id}", settings.FoodId, settings.FoodHq, false, (itemId, hq) =>
        {
            settings.FoodId = itemId;
            settings.FoodHq = hq;
        });
        changed |= DrawConsumable("藥水", $"potion-{id}", settings.PotionId, settings.PotionHq, true, (itemId, hq) =>
        {
            settings.PotionId = itemId;
            settings.PotionHq = hq;
        });

        if (changed)
            C.Save();
    }

    private static bool DrawConsumable(string label, string id, uint selectedId, bool selectedHq, bool potion, Action<uint, bool> select)
    {
        var preview = selectedId switch
        {
            0 => "Artisan 原設定",
            1 => "不使用",
            _ => $"{(selectedHq ? " " : "")}{ItemName(selectedId)}",
        };
        var changed = false;
        ImGui.SetNextItemWidth(280);
        if (ImGui.BeginCombo($"{label}##{id}", preview))
        {
            Select("Artisan 原設定", 0, true);
            Select("不使用", 1, false);
            foreach (var itemId in potion ? availablePotsNq : availableFoodNq)
                Select(ItemName(itemId), itemId, false);
            foreach (var itemId in potion ? availablePotsHq : availableFoodHq)
                Select($" {ItemName(itemId)}", itemId, true);
            ImGui.EndCombo();
        }
        return changed;

        void Select(string text, uint itemId, bool hq)
        {
            if (ImGui.Selectable($"{text}##{id}-{itemId}-{hq}", selectedId == itemId && (itemId <= 1 || selectedHq == hq)))
            {
                select(itemId, hq);
                changed = true;
            }
        }
    }

    private static void RefreshAvailableOptions()
    {
        if (!P.Artisan.Installed)
            return;
        availableFoodNq = P.Artisan.GetAvailableFood(false).Distinct().Order().ToArray();
        availableFoodHq = P.Artisan.GetAvailableFood(true).Distinct().Order().ToArray();
        availablePotsNq = P.Artisan.GetAvailablePots(false).Distinct().Order().ToArray();
        availablePotsHq = P.Artisan.GetAvailablePots(true).Distinct().Order().ToArray();
        availableSolvers = CosmicHelper.SheetMissionDict.Values
            .SelectMany(mission => mission.Crafts_Main.Keys)
            .Distinct()
            .SelectMany(recipeId => P.Artisan.GetAvailableSolvers(recipeId))
            .Distinct()
            .Order()
            .ToArray();
        scanned = true;
    }

    private static int ApplyProfile(int profileId, int missionType, int progress, int quality)
    {
        var applied = 0;
        foreach (var (missionId, mission) in CosmicHelper.SheetMissionDict)
        {
            var isExpert = mission.Attributes.HasFlag(MissionAttributes.ExpertCraft);
            if (mission.Crafts_Main.Count == 0 || missionType == 1 && isExpert || missionType == 2 && !isExpert)
                continue;
            if (!C.MissionConfig.TryGetValue(missionId, out var missionConfig))
                continue;

            foreach (var (recipeId, craft) in mission.Crafts_Main)
            {
                if (progress > 0 && craft.Progress != progress || quality > 0 && craft.Quality != quality)
                    continue;
                if (!missionConfig.CraftSettings.TryGetValue(recipeId, out var settings))
                    missionConfig.CraftSettings[recipeId] = settings = new();
                settings.CraftProfileId = profileId;
                applied++;
            }
        }
        return applied;
    }

    private static string ItemName(uint itemId)
        => Svc.Data.GetExcelSheet<Item>().TryGetRow(itemId, out var item) && !string.IsNullOrEmpty(item.Name.ToString())
            ? item.Name.ToString()
            : $"物品 {itemId}";

    private static void EnsureProfiles()
    {
        C.CraftProfiles ??= new();
        if (!C.CraftProfiles.ContainsKey(0))
            C.CraftProfiles[0] = new CraftProfile { Id = 0, Name = "預設" };
        foreach (var (id, profile) in C.CraftProfiles)
        {
            profile.Id = id;
            profile.Settings ??= new();
        }
    }
}
