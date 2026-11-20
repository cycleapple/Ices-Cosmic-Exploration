using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Pictomancy;
using System.Collections.Generic;
using static ICE.ConfigFiles.Config;

namespace ICE.Ui.MainUi.Settings
{
    internal class Character_Settings
    {
        public static void Draw()
        {
            // Register current character whenever this panel is open
            if (Player.Available)
            {
                var cid = (ulong)Player.CID;
                C.KnownCharacters[cid] = Player.Name;
            }

            if (ImGui.BeginTabBar("CharacterSettingsTabs"))
            {
                if (ImGui.BeginTabItem("Global"))
                {
                    if (ImGui.BeginChild("Global Character Settings"))
                    {
                        RepairSettings(null);
                        ImGui.Separator();
                        ArtisanSettingsV2(null);
                        ImGui.Separator();
                        MountSelection(null);
                        ImGui.Separator();
                        RelicJobSwap(null);
                    }
                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }

                foreach (var (cid, name) in C.KnownCharacters.ToList())
                {
                    bool tabOpen = true;
                    if (ImGui.BeginTabItem($"{name}##cid_{cid}", ref tabOpen))
                    {
                        if (!C.CharacterOverrides.ContainsKey(cid))
                        {
                            C.CharacterOverrides[cid] = new();
                            C.Save();
                        }

                        if (ImGui.BeginChild("Character Specific"))
                        {
                            var ov = C.CharacterOverrides[cid];
                            RepairSettings(ov);
                            ImGui.Separator();
                            ArtisanSettingsV2(ov);
                            ImGui.Separator();
                            MountSelection(ov);
                            ImGui.Separator();
                            RelicJobSwap(ov);

                        }
                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }

                    if (!tabOpen)
                    {
                        C.CharacterOverrides.Remove(cid);
                        C.KnownCharacters.Remove(cid);
                        C.Save();
                        break;
                    }
                }

                ImGui.EndTabBar();
            }
        }

        // -------------------------------------------------------------------------
        // Override helper — renders a small checkbox to toggle the override, then
        // the actual control (greyed out when not overriding).
        // drawControl receives the current effective value and should render the
        // ImGui control, write back into `ov` directly, and return true if changed.
        // -------------------------------------------------------------------------
        private static void OverrideField<T>(string id, T globalVal, T? overrideVal, Action<T?> setOverride, Action<T> drawControl) where T : struct
        {
            bool hasOverride = overrideVal.HasValue;
            if (ImGui.Checkbox($"##ov_{id}", ref hasOverride))
            {
                setOverride(hasOverride ? globalVal : null);
                C.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hasOverride
                    ? "Override active — click to inherit from Global"
                    : "Click to override this setting for this character");

            ImGui.SameLine();

            using (ImRaii.Disabled(!hasOverride))
                drawControl(overrideVal ?? globalVal);
        }

        // Reference-type variant (string, Global_Artisan)
        private static void OverrideFieldRef<T>(string id, T globalVal, T? overrideVal, Action<T?> setOverride, Action<T> drawControl) where T : class
        {
            bool hasOverride = overrideVal != null;
            if (ImGui.Checkbox($"##ov_{id}", ref hasOverride))
            {
                // For reference types, seed with a clone of global so the user starts
                // from the current global value rather than blank.
                setOverride(hasOverride ? CloneRef(globalVal) : null);
                C.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hasOverride
                    ? "Override active — click to inherit from Global"
                    : "Click to override this setting for this character");

            ImGui.SameLine();

            using (ImRaii.Disabled(!hasOverride))
                drawControl(overrideVal ?? globalVal);
        }

        // Shallow-clone helper for Global_Artisan seeding
        private static T CloneRef<T>(T source) where T : class
        {
            if (source is Global_Artisan a)
                return (new Global_Artisan
                {
                    SolverType = a.SolverType,
                    FoodId = a.FoodId,
                    FoodHQ = a.FoodHQ,
                    PotionId = a.PotionId,
                    PotionHQ = a.PotionHQ,
                    ManualId = a.ManualId,
                    SquadronManual = a.SquadronManual,
                } as T)!;
            return source;
        }

        // -------------------------------------------------------------------------
        // Repair Settings
        // -------------------------------------------------------------------------
        private static void RepairSettings(CharacterOverride? ov)
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Hammer, "修理設定");
            ImGui.Dummy(new Vector2(0, 5));

            if (ov == null)
            {
                // ── Global ──────────────────────────────────────────────────────
                bool repairAtVendor = C.RepairAtVendor;
                if (ImGui.Checkbox("向 NPC 修理", ref repairAtVendor))
                { C.RepairAtVendor = repairAtVendor; C.Save(); }

                using (ImRaii.Disabled(repairAtVendor))
                {
                    bool selfRepairGather = C.SelfRepairGather;
                    if (ImGui.Checkbox("採集職自行修理", ref selfRepairGather))
                    { C.SelfRepairGather = selfRepairGather; C.Save(); }

                    bool selfRepairCrafter = C.SelfRepairCrafter;
                    if (ImGui.Checkbox("製作職業自行修理", ref selfRepairCrafter))
                    { C.SelfRepairCrafter = selfRepairCrafter; C.Save(); }
                }

                float repairAmount = C.RepairPercent;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("###Repair %", ref repairAmount, 0f, 99f, "%.0f%%"))
                {
                    if (C.RepairPercent != (int)repairAmount)
                    { C.RepairPercent = (int)repairAmount; C.SaveDebounced(); }
                }

                bool repairAll = C.RepairAllGear;
                if (ImGui.Checkbox("Repair all gear in bag", ref repairAll))
                { C.RepairAllGear = repairAll; C.Save(); }

                bool stopDarkMatter = C.Stop_DarkMatter;
                if (ImGui.Checkbox("Stop when below x dark matter", ref stopDarkMatter))
                { C.Stop_DarkMatter = stopDarkMatter; C.Save(); }

                int minDM = C.Minimum_DarkMatter;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputInt("Minimum Grade 8 Dark Matter", ref minDM, 1, 10))
                { C.Minimum_DarkMatter = minDM; C.SaveDebounced(); }
            }
            else
            {
                // ── Per-character override ───────────────────────────────────────
                OverrideField("RepairAtVendor", C.RepairAtVendor, ov.RepairAtVendor,
                    v => ov.RepairAtVendor = v,
                    current => {
                        bool v = current;
                        if (ImGui.Checkbox("向 NPC 修理", ref v) && ov.RepairAtVendor.HasValue)
                        { ov.RepairAtVendor = v; C.Save(); }
                    });

                bool effectiveVendor = ov.RepairAtVendor ?? C.RepairAtVendor;
                using (ImRaii.Disabled(effectiveVendor))
                {
                    OverrideField("SelfRepairGather", C.SelfRepairGather, ov.SelfRepairGather,
                        v => ov.SelfRepairGather = v,
                        current => {
                            bool v = current;
                            if (ImGui.Checkbox("採集職自行修理", ref v) && ov.SelfRepairGather.HasValue)
                            { ov.SelfRepairGather = v; C.Save(); }
                        });

                    OverrideField("SelfRepairCrafter", C.SelfRepairCrafter, ov.SelfRepairCrafter,
                        v => ov.SelfRepairCrafter = v,
                        current => {
                            bool v = current;
                            if (ImGui.Checkbox("製作職業自行修理", ref v) && ov.SelfRepairCrafter.HasValue)
                            { ov.SelfRepairCrafter = v; C.Save(); }
                        });
                }

                OverrideField("RepairPercent", C.RepairPercent, ov.RepairPercent,
                    v => ov.RepairPercent = v,
                    current => {
                        float f = current;
                        ImGui.SetNextItemWidth(150);
                        if (ImGui.SliderFloat("###Repair %", ref f, 0f, 99f, "%.0f%%") && ov.RepairPercent.HasValue)
                        { ov.RepairPercent = (int)f; C.SaveDebounced(); }
                    });

                OverrideField("RepairAllGear", C.RepairAllGear, ov.RepairAllGear,
                    v => ov.RepairAllGear = v,
                    current => {
                        bool v = current;
                        if (ImGui.Checkbox("Repair all gear in bag", ref v) && ov.RepairAllGear.HasValue)
                        { ov.RepairAllGear = v; C.Save(); }
                    });

                // Stop_DarkMatter is global-only — no per-character override
                using (ImRaii.Disabled(true))
                {
                    bool stopDM = C.Stop_DarkMatter;
                    ImGui.Checkbox("Stop when below x dark matter (Global)", ref stopDM);
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("This setting is global and shared across all characters.\nEdit it on the Global tab.");

                OverrideField("Minimum_DarkMatter", C.Minimum_DarkMatter, ov.Minimum_DarkMatter,
                    v => ov.Minimum_DarkMatter = v,
                    current => {
                        int v = current;
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.InputInt("Minimum Grade 8 Dark Matter", ref v, 1, 10) && ov.Minimum_DarkMatter.HasValue)
                        { ov.Minimum_DarkMatter = v; C.SaveDebounced(); }
                    });
            }

            PlayerHelper.GetItemCount(Utils.DarkMatter_8Id, out var dmCount);
            ImGui.Text($"Currently have: {dmCount:N0} Grade 8 Dark Matter");
        }

        // -------------------------------------------------------------------------
        // Artisan Settings
        // -------------------------------------------------------------------------
        private static void ArtisanSettingsV2(CharacterOverride? ov)
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Wrench, "Global Artisan Settings");
            ImGui.Dummy(new Vector2(0, 5));

            // Resolve which artisan objects we're actually editing
            var craft_Standard = ov?.Artisan_GlobalStandard ?? C.Artisan_GlobalStandard;
            var craft_Expert = ov?.Artisan_GlobalExpert ?? C.Artisan_GlobalExpert;

            List<ArtisanCraftType> global_StandardModes = new()
            {
                ArtisanCraftType.Default,
                ArtisanCraftType.ProgressOnly,
                ArtisanCraftType.Standard,
                ArtisanCraftType.Raphael,
            };
            List<ArtisanCraftType> global_ExpertModes = new()
            {
                ArtisanCraftType.Default,
                ArtisanCraftType.Expert,
                ArtisanCraftType.Raphael,
            };

            // If we're in character mode and the override objects don't exist yet we
            // show an "Override Artisan settings" master toggle instead of the full table.
            if (ov != null)
            {
                bool hasStandardOverride = ov.Artisan_GlobalStandard != null;
                bool hasExpertOverride = ov.Artisan_GlobalExpert != null;

                if (ImGui.Checkbox("Override Standard Artisan settings##ovStd", ref hasStandardOverride))
                {
                    ov.Artisan_GlobalStandard = hasStandardOverride ? CloneRef(C.Artisan_GlobalStandard) : null;
                    C.Save();
                }
                ImGui.SameLine();
                if (ImGui.Checkbox("Override Expert Artisan settings##ovExp", ref hasExpertOverride))
                {
                    ov.Artisan_GlobalExpert = hasExpertOverride ? CloneRef(C.Artisan_GlobalExpert) : null;
                    C.Save();
                }
                ImGui.Dummy(new Vector2(0, 3));
            }

            #region Labels

            string GetSolverLabel(ArtisanCraftType type) => type switch
            {
                ArtisanCraftType.Default => "預設",
                ArtisanCraftType.Raphael => "Raphael Solver",
                ArtisanCraftType.ProgressOnly => "Progress Only Solver",
                ArtisanCraftType.Standard => "Standard Solver",
                ArtisanCraftType.Expert => "Expert Recipe Solver",
                _ => "Unknown"
            };

            string GetFoodLabel(uint foodId)
            {
                if (foodId == 0) return "預設";
                var item = ConsumableInfo.CrafterFood.FirstOrDefault(x => x.Id == foodId);
                PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                return BuildItemLabel(item.Name, nq, hq);
            }
            string GetPotionLabel(uint potionId)
            {
                if (potionId == 0) return "預設";
                var item = ConsumableInfo.Pots.FirstOrDefault(x => x.Id == potionId);
                PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                return BuildItemLabel(item.Name, nq, hq);
            }
            string GetManualLabel(uint manualId)
            {
                if (manualId == 0) return "預設";
                var item = ConsumableInfo.Manuals.FirstOrDefault(x => x.Id == manualId);
                PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                return BuildItemLabel(item.Name, nq, 0);
            }
            string GetSquadManualLabel(uint squadManualId)
            {
                if (squadManualId == 0) return "預設";
                var item = ConsumableInfo.SquadronManuals.FirstOrDefault(x => x.Id == squadManualId);
                PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                return BuildItemLabel(item.Name, nq, 0);
            }
            string BuildItemLabel(string name, int nqCount, int hqCount)
            {
                var parts = new List<string>();
                if (hqCount > 0) parts.Add($"{(char)0xE03C} {name} [x{hqCount}]");
                if (nqCount > 0) parts.Add($"{name} [x{nqCount}]");
                return string.Join(" / ", parts);
            }

            var std_FoodLabel = GetFoodLabel(craft_Standard.FoodId);
            var exp_FoodLabel = GetFoodLabel(craft_Expert.FoodId);
            var std_PotionLabel = GetPotionLabel(craft_Standard.PotionId);
            var exp_PotionLabel = GetPotionLabel(craft_Expert.PotionId);
            var std_ManualLabel = GetManualLabel(craft_Standard.ManualId);
            var exp_ManualLabel = GetManualLabel(craft_Expert.ManualId);
            var std_SquadLabel = GetSquadManualLabel(craft_Standard.SquadronManual);
            var exp_SquadLabel = GetSquadManualLabel(craft_Expert.SquadronManual);

            float std_ComboWidth = new[]
            {
                std_FoodLabel, std_PotionLabel, std_ManualLabel, std_SquadLabel,
                GetSolverLabel(craft_Standard.SolverType),
            }.Max(l => ImGui.CalcTextSize(l).X + ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ScrollbarSize + 10);

            float exp_ComboWidth = new[]
            {
                exp_FoodLabel, exp_PotionLabel, exp_ManualLabel, exp_SquadLabel,
                GetSolverLabel(craft_Expert.SolverType),
            }.Max(l => ImGui.CalcTextSize(l).X + ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ScrollbarSize + 10);

            #endregion

            // When in character mode and neither override is active, grey out the table
            bool tableDisabled = ov != null && ov.Artisan_GlobalStandard == null && ov.Artisan_GlobalExpert == null;
            using (ImRaii.Disabled(tableDisabled))
            {
                if (ImGui.BeginTable("ArtisanSettings", 3,
                    ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("");
                    ImGui.TableSetupColumn("Standard Craft Settings");
                    ImGui.TableSetupColumn("Expert Craft Settings");
                    ImGui.TableHeadersRow();

                    bool stdDisabled = ov != null && ov.Artisan_GlobalStandard == null;
                    bool expDisabled = ov != null && ov.Artisan_GlobalExpert == null;

                    void SaveArtisan()
                    {
                        if (ov == null)
                        {

                        }
                        C.Save();
                    }

                    #region Solver Row
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Solver Type");

                    ImGui.TableNextColumn();
                    using (ImRaii.Disabled(stdDisabled))
                    {
                        ImGui.SetNextItemWidth(std_ComboWidth);
                        if (ImGui.BeginCombo("##StandardSolverType", GetSolverLabel(craft_Standard.SolverType)))
                        {
                            foreach (var type in global_StandardModes)
                            {
                                bool sel = craft_Standard.SolverType == type;
                                if (ImGui.Selectable(GetSolverLabel(type), sel))
                                { 
                                    craft_Standard.SolverType = type; 
                                    SaveArtisan(); 
                                }
                                if (sel) ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }
                    }

                    ImGui.TableNextColumn();
                    using (ImRaii.Disabled(expDisabled))
                    {
                        ImGui.SetNextItemWidth(exp_ComboWidth);
                        if (ImGui.BeginCombo("##ExpertSolverType", GetSolverLabel(craft_Expert.SolverType)))
                        {
                            foreach (var type in global_ExpertModes)
                            {
                                bool sel = craft_Expert.SolverType == type;
                                if (ImGui.Selectable(GetSolverLabel(type), sel))
                                { 
                                    craft_Expert.SolverType = type; 
                                    SaveArtisan(); 
                                }
                                if (sel) 
                                    ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }
                    }
                    #endregion

                    bool supportedArtisan = P.Artisan.UpdatedArtisan();
                    if (supportedArtisan)
                    {
                        #region Food Row
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("食物");

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(stdDisabled))
                        {
                            ImGui.SetNextItemWidth(std_ComboWidth);
                            if (ImGui.BeginCombo("##StandardFood", std_FoodLabel))
                            {
                                if (ImGui.Selectable("預設", craft_Standard.FoodId == 0))
                                { 
                                    craft_Standard.FoodId = 0; 
                                    craft_Standard.FoodHQ = false; 
                                    SaveArtisan(); 
                                }
                                if (craft_Standard.FoodId == 0) 
                                    ImGui.SetItemDefaultFocus();
                                ImGui.Separator();
                                foreach (var item in ConsumableInfo.CrafterFood)
                                {
                                    PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                                    PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                                    if (nq == 0 && hq == 0) 
                                        continue;
                                    bool sel = craft_Standard.FoodId == item.Id;
                                    if (ImGui.Selectable(BuildItemLabel(item.Name, nq, hq) + $"###{item.Id}", sel))
                                    { 
                                        craft_Standard.FoodId = item.Id; 
                                        craft_Standard.FoodHQ = hq > 0; 
                                        SaveArtisan(); 
                                    }
                                    if (sel) 
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                        }

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(expDisabled))
                        {
                            ImGui.SetNextItemWidth(exp_ComboWidth);
                            if (ImGui.BeginCombo("##ExpertFood", exp_FoodLabel))
                            {
                                if (ImGui.Selectable("預設", craft_Expert.FoodId == 0))
                                { 
                                    craft_Expert.FoodId = 0; 
                                    craft_Expert.FoodHQ = false; 
                                    SaveArtisan(); 
                                }
                                if (craft_Expert.FoodId == 0) 
                                    ImGui.SetItemDefaultFocus();
                                ImGui.Separator();
                                foreach (var item in ConsumableInfo.CrafterFood)
                                {
                                    PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                                    PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                                    if (nq == 0 && hq == 0) 
                                        continue;
                                    bool sel = craft_Expert.FoodId == item.Id;
                                    if (ImGui.Selectable(BuildItemLabel(item.Name, nq, hq) + $"###{item.Id}", sel))
                                    { 
                                        craft_Expert.FoodId = item.Id; 
                                        craft_Expert.FoodHQ = hq > 0; 
                                        SaveArtisan(); 
                                    }
                                    if (sel) 
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                        }
                        #endregion

                        #region Potion Row
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Potions");

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(stdDisabled))
                        {
                            ImGui.SetNextItemWidth(std_ComboWidth);
                            if (ImGui.BeginCombo("##StandardPotion", std_PotionLabel))
                            {
                                if (ImGui.Selectable("預設", craft_Standard.PotionId == 0))
                                { 
                                    craft_Standard.PotionId = 0; 
                                    craft_Standard.PotionHQ = false; 
                                    SaveArtisan(); 
                                }
                                if (craft_Standard.PotionId == 0) 
                                    ImGui.SetItemDefaultFocus();
                                ImGui.Separator();
                                foreach (var item in ConsumableInfo.Pots)
                                {
                                    PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                                    PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                                    if (nq == 0 && hq == 0) 
                                        continue;
                                    bool sel = craft_Standard.PotionId == item.Id;
                                    if (ImGui.Selectable(BuildItemLabel(item.Name, nq, hq) + $"###{item.Id}", sel))
                                    { 
                                        craft_Standard.PotionId = item.Id; 
                                        craft_Standard.PotionHQ = hq > 0; 
                                        SaveArtisan(); 
                                    }
                                    if (sel) 
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                        }

                        ImGui.TableNextColumn();
                        using (ImRaii.Disabled(expDisabled))
                        {
                            ImGui.SetNextItemWidth(exp_ComboWidth);
                            if (ImGui.BeginCombo("##ExpertPotion", exp_PotionLabel))
                            {
                                if (ImGui.Selectable("預設", craft_Expert.PotionId == 0))
                                { 
                                    craft_Expert.PotionId = 0; 
                                    craft_Expert.PotionHQ = false; 
                                    SaveArtisan(); 
                                }
                                if (craft_Expert.PotionId == 0) 
                                    ImGui.SetItemDefaultFocus();
                                ImGui.Separator();
                                foreach (var item in ConsumableInfo.Pots)
                                {
                                    PlayerHelper.GetItemCount(item.Id, out var nq, includeHq: false, includeNq: true);
                                    PlayerHelper.GetItemCount(item.Id, out var hq, includeHq: true, includeNq: false);
                                    if (nq == 0 && hq == 0) 
                                        continue;
                                    bool sel = craft_Expert.PotionId == item.Id;
                                    if (ImGui.Selectable(BuildItemLabel(item.Name, nq, hq) + $"###{item.Id}", sel))
                                    { 
                                        craft_Expert.PotionId = item.Id; 
                                        craft_Expert.PotionHQ = hq > 0; 
                                        SaveArtisan(); 
                                    }
                                    if (sel) 
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                        }
                        #endregion

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextWrapped("手冊設定請在 Artisan 調整。");
                        ImGui.TableNextColumn();
                        if ((craft_Standard.ManualId != 0 || craft_Standard.SquadronManual != 0)
                            && ImGui.Button("清除標準配方手冊覆寫"))
                        { craft_Standard.ManualId = 0; craft_Standard.SquadronManual = 0; SaveArtisan(); }
                        ImGui.TableNextColumn();
                        if ((craft_Expert.ManualId != 0 || craft_Expert.SquadronManual != 0)
                            && ImGui.Button("清除專家配方手冊覆寫"))
                        { craft_Expert.ManualId = 0; craft_Expert.SquadronManual = 0; SaveArtisan(); }

                    }

                    ImGui.EndTable();
                }
            }
        }

        // -------------------------------------------------------------------------
        // Mount Selection
        // -------------------------------------------------------------------------
        private static bool _visualizeRadius = false;
        private static bool _visualizeDismountRadius = false;
        private static Dictionary<uint, string> _availableMounts = new();
        private static string _mountSearchText = "";
        private static int _mountDisplayOffset = 0;
        private static int _mountItemsPerPage = 10;

        private static unsafe void MountSelection(CharacterOverride? ov)
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Feather, "坐騎設定");
            ImGui.Dummy(new Vector2(0, 5));

            // Resolve effective values for display
            uint effectiveMountId = ov?.MountId ?? C.MountId;
            string effectiveMountName = ov?.MountName ?? C.MountName;
            bool effectiveMountOutside = C.UseMountOutsideMission;
            bool effectiveMountInMission = C.UseMountInMission;
            float effectiveMinMountRange = C.MountRadius;
            float effectiveDismountRange = C.DismountRadius;

            // Mount picker button
            if (ov == null)
            {
                if (ImGui.Button("選擇坐騎"))
                    OpenMountPopup();
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"坐騎：{C.MountName}");
            }
            else
            {
                bool hasMountOverride = ov.MountId.HasValue;
                if (ImGui.Checkbox("##ovMount", ref hasMountOverride))
                {
                    if (hasMountOverride) { ov.MountId = C.MountId; ov.MountName = C.MountName; }
                    else { ov.MountId = null; ov.MountName = null; }
                    C.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasMountOverride
                        ? "Override active — click to inherit from Global"
                        : "Click to override mount for this character");
                ImGui.SameLine();

                using (ImRaii.Disabled(!hasMountOverride))
                {
                    if (ImGui.Button("選擇坐騎##charMount"))
                        OpenMountPopup();
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"Mount: {effectiveMountName}");
                }
            }

            // Shared popup — writes to ov if present, else to C directly
            if (ImGui.BeginPopup("Mount Options"))
            {
                ImGui.InputText("搜尋", ref _mountSearchText, 100);

                var filtered = _availableMounts
                    .Where(kvp => string.IsNullOrEmpty(_mountSearchText) ||
                                  kvp.Value.Contains(_mountSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int total = filtered.Count;
                int maxOffset = Math.Max(0, total - _mountItemsPerPage);
                _mountDisplayOffset = Math.Min(_mountDisplayOffset, maxOffset);

                foreach (var mount in filtered.Skip(_mountDisplayOffset).Take(_mountItemsPerPage))
                {
                    if (ImGui.Selectable($"{mount.Value}##{mount.Key}"))
                    {
                        if (ov != null && ov.MountId.HasValue)
                        { ov.MountId = mount.Key; ov.MountName = mount.Value; }
                        else if (ov == null)
                        { C.MountId = mount.Key; C.MountName = mount.Value; }
                        C.Save();
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.Separator();
                if (ImGui.Button("上一頁") && _mountDisplayOffset > 0)
                    _mountDisplayOffset = Math.Max(0, _mountDisplayOffset - _mountItemsPerPage);
                ImGui.SameLine();
                ImGui.Text($"{_mountDisplayOffset + 1}-{Math.Min(_mountDisplayOffset + _mountItemsPerPage, total)} of {total}");
                ImGui.SameLine();
                if (ImGui.Button("下一頁") && _mountDisplayOffset < maxOffset)
                    _mountDisplayOffset = Math.Min(maxOffset, _mountDisplayOffset + _mountItemsPerPage);

                ImGui.EndPopup();
            }

            // Bool / float fields
            if (ov == null)
            {
                bool mountOutside = C.UseMountOutsideMission;
                if (ImGui.Checkbox("任務外使用坐騎", ref mountOutside))
                { C.UseMountOutsideMission = mountOutside; C.Save(); }

                bool mountInMission = C.UseMountInMission;
                if (ImGui.Checkbox("任務中使用坐騎", ref mountInMission))
                { C.UseMountInMission = mountInMission; C.Save(); }

                float minRange = C.MountRadius;
                ImGui.SetNextItemWidth(100);
                if (ImGui.DragFloat("最小騎乘距離", ref minRange, 1))
                { C.MountRadius = minRange; C.Save(); }
                ImGui.SameLine();
                ImGui.Checkbox("顯示範圍", ref _visualizeRadius);

                float dismount = C.DismountRadius;
                ImGui.SetNextItemWidth(100);
                if (ImGui.DragFloat("下坐騎目標距離", ref dismount, 1))
                { C.DismountRadius = dismount; C.Save(); }
                ImGui.SameLine();
                ImGui.Checkbox("顯示下坐騎範圍", ref _visualizeDismountRadius);
            }
            else
            {
                // These four are global-only — always read/write C regardless of character tab.
                // Show them as editable but with a tooltip clarifying they affect all characters.
                bool mountOutside = C.UseMountOutsideMission;
                if (ImGui.Checkbox("任務外使用坐騎##global", ref mountOutside))
                { 
                    C.UseMountOutsideMission = mountOutside; 
                    C.Save(); 
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Global setting — applies to all characters.");

                bool mountInMission = C.UseMountInMission;
                if (ImGui.Checkbox("任務中使用坐騎##global", ref mountInMission))
                { C.UseMountInMission = mountInMission; C.Save(); }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Global setting — applies to all characters.");

                float minRange = C.MountRadius;
                ImGui.SetNextItemWidth(100);
                if (ImGui.DragFloat("最小騎乘距離##global", ref minRange, 1))
                { 
                    C.MountRadius = minRange; 
                    C.Save(); 
                }
                if (ImGui.IsItemHovered()) 
                    ImGui.SetTooltip("Global setting — applies to all characters.");
                ImGui.SameLine();
                ImGui.Checkbox("顯示範圍", ref _visualizeRadius);

                float dismount = C.DismountRadius;
                ImGui.SetNextItemWidth(100);
                if (ImGui.DragFloat("下坐騎目標距離##global", ref dismount, 1))
                { 
                    C.DismountRadius = dismount; 
                    C.Save(); 
                }
                if (ImGui.IsItemHovered()) 
                    ImGui.SetTooltip("Global setting — applies to all characters.");
                ImGui.SameLine();
                ImGui.Checkbox("顯示下坐騎範圍", ref _visualizeDismountRadius);
            }

            // Pictomancy visualisation (always uses resolved values)
            using (var drawList = PictoService.Draw())
            {
                if (drawList == null) return;
                var playerPos = Player.Position;
                if (_visualizeRadius)
                    PictoService.VfxRenderer.AddCircle("Mount_Radius Circle", playerPos,
                        C.MountRadius, Utils.FromUintABGR(2616716297));
                if (_visualizeDismountRadius)
                    PictoService.VfxRenderer.AddCircle("Dismount_Radius Circle", playerPos,
                        C.DismountRadius, Utils.FromUintABGR(2601121571));
            }
        }

        private static unsafe void OpenMountPopup()
        {
            _availableMounts.Clear();
            _availableMounts[0] = "隨機坐騎";
            var mountSheet = Svc.Data.GetExcelSheet<Mount>();
            foreach (var mountItem in mountSheet)
            {
                if (!PlayerState.Instance()->IsMountUnlocked(mountItem.RowId)) continue;
                string name = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(mountItem.Singular.ToString().ToLower());
                _availableMounts[mountItem.RowId] = name;
            }
            _mountSearchText = "";
            _mountDisplayOffset = 0;
            ImGui.OpenPopup("Mount Options");
        }

        // -------------------------------------------------------------------------
        // Relic Job Swap
        // -------------------------------------------------------------------------
        private static void RelicJobSwap(CharacterOverride? ov)
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Toolbox, "Class Swap");
            ImGui.Dummy(new(0, 5));
            if (ov == null)
            {
                bool swapJobs = C.Relic_SwapJob;
                if (ImGui.Checkbox("Swap jobs when turning in relic", ref swapJobs))
                { 
                    C.Relic_SwapJob = swapJobs; 
                    C.Save(); 
                }

                string currentJobName = BattleJobs.FirstOrDefault(x => x.Value == C.Relic_BattleJob).Key ?? "無";
                ImGui.SetNextItemWidth(200);
                if (ImGui.BeginCombo("Battle Job##relicJob", currentJobName))
                {
                    foreach (var (jobName, jobId) in BattleJobs)
                    {
                        bool sel = C.Relic_BattleJob == jobId;
                        if (ImGui.Selectable(jobName, sel))
                        { 
                            C.Relic_BattleJob = jobId; 
                            C.Save(); 
                        }
                        if (sel) 
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                bool useStylist = C.Relic_Stylist;
                if (ImGui.Checkbox("Use Stylist to re-equip tools", ref useStylist))
                {
                    C.Relic_Stylist = useStylist;
                    C.Save();
                }
            }
            else
            {
                OverrideField("Relic_SwapJob", C.Relic_SwapJob, ov.Relic_SwapJob,
                    v => ov.Relic_SwapJob = v,
                    current => {
                        bool v = current;
                        if (ImGui.Checkbox("Swap jobs when turning in relic", ref v) && ov.Relic_SwapJob.HasValue)
                        { 
                            ov.Relic_SwapJob = v; 
                            C.Save(); 
                        }
                    });

                OverrideField("Relic_BattleJob", C.Relic_BattleJob, ov.Relic_BattleJob,
                    v => ov.Relic_BattleJob = v,
                    current => {
                        string currentJobName = BattleJobs.FirstOrDefault(x => x.Value == current).Key ?? "無";
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.BeginCombo("Battle Job##relicJobOv", currentJobName))
                        {
                            foreach (var (jobName, jobId) in BattleJobs)
                            {
                                bool sel = current == jobId;
                                if (ImGui.Selectable(jobName, sel) && ov.Relic_BattleJob.HasValue)
                                { 
                                    ov.Relic_BattleJob = jobId; 
                                    C.Save(); 
                                }
                                if (sel) ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndCombo();
                        }
                    });

                OverrideField("Relic_Stylist", C.Relic_Stylist, ov.Relic_Stylist,
                    v => ov.Relic_Stylist = v,
                    current =>
                    {
                        bool v = current;
                        if (ImGui.Checkbox("Use Stylist to re-equip tools", ref v) && ov.Relic_Stylist.HasValue)
                        {
                            ov.Relic_Stylist = v;
                            C.Save();
                        }
                    });
            }
        }

        // -------------------------------------------------------------------------
        // Battle job lookup table
        // -------------------------------------------------------------------------
        private static readonly Dictionary<string, uint> BattleJobs = new()
        {
            // Tanks
            { "騎士",    19 }, { "戰士",     21 }, { "暗黑騎士", 32 }, { "絕槍戰士",  37 },
            // Healers
            { "白魔法師", 24 }, { "學者",     28 }, { "占星術士", 33 }, { "賢者",        40 },
            // Melee DPS
            { "武僧",       20 }, { "龍騎士",     22 }, { "忍者",       30 }, { "武士",     34 },
            { "鐮刀師",     39 }, { "蝰蛇劍士",       41 },
            // Physical Ranged DPS
            { "吟遊詩人",       23 }, { "機工士",   31 }, { "舞者",      38 },
            // Magical Ranged DPS
            { "黑魔法師", 25 }, { "召喚師",    27 }, { "赤魔法師",    35 }, { "繪靈法師", 42 },
        };
    }
}
