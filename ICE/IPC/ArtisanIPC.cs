using ECommons.EzIpcManager;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ICE.Utilities.Cosmic_Helper;
using System.Collections.Generic;
using static ICE.ConfigFiles.Config.MissionSettings;

namespace ICE.IPC
{
    public class ArtisanIPC
    {
        public const string Name = "Artisan";
        public ArtisanIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);

        [EzIPC] public Func<bool> IsBusy;
        [EzIPC] public Func<bool> GetEnduranceStatus;
        [EzIPC] public Action<bool> SetEnduranceStatus;
        [EzIPC] public Func<bool> IsListRunning;
        [EzIPC] public Func<bool> IsListPaused;
        [EzIPC] public Action<bool> SetListPause;
        [EzIPC] public Func<bool> GetStopRequest;
        [EzIPC] public Action<bool> SetStopRequest;
        [EzIPC] public Action<ushort, int> CraftItem;
        [EzIPC] public Action<ushort, uint, uint, uint, uint> AssignRecipie;

        [EzIPC] public Action<uint, string, bool> ChangeSolver;
        [EzIPC] public Action<uint> SetTempSolverBackToNormal;

        [EzIPC] public Action<uint, uint, bool, bool> ChangeFood;
        [EzIPC] public Action<uint> SetTempFoodBackToNormal;

        [EzIPC] public Action<uint, uint, bool, bool> ChangePotion;
        [EzIPC] public Action<uint> SetTempPotionBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeManual;
        [EzIPC] public Action<uint> SetTempManualBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeSquadronManual;
        [EzIPC] public Action<uint> SetTempSquadronManualBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeExpertProfileID;
        [EzIPC] public Action<uint> SetTempExpertProfileIDBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeExpertMaxSteadyUses;
        [EzIPC] public Action<uint> SetTempExpertMaxSteadyUsesBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeExpertMaxMaterialMiracleUses;
        [EzIPC] public Action<uint> SetTempExpertMaxMaterialMiracleUsesBackToNormal;

        [EzIPC] public Action<uint, uint, bool> ChangeExpertMinimumStepsBeforeMiracle;
        [EzIPC] public Action<uint> SetTempExpertMinimumStepsBeforeMiracleBackToNormal;

        [EzIPC] public Action<uint, bool> ChangeStandardMaxMaterialMiracleUses;
        [EzIPC] public Action SetTempStandardMaxMaterialMiracleUsesBackToNormal;

        [EzIPC] public Action<uint, bool> ChangeStandardMinimumStepsBeforeMiracle;
        [EzIPC] public Action SetTempStandardMinimumStepsBeforeMiracleBackToNormal;

        [EzIPC] public Func<List<(string, int)>> ReturnMacroInfo;

        [EzIPC] public Func<uint, string, bool> SetTemporarySolver;
        [EzIPC] public Func<uint, uint, bool, bool> SetTemporaryFood;
        [EzIPC] public Func<uint, uint, bool, bool> SetTemporaryPotion;
        [EzIPC] public Action<uint> ClearTemporaryRecipeSettings;
        [EzIPC] public Action ClearAllTemporarySettings;
        [EzIPC] public Func<uint, string[]> GetAvailableSolvers;
        [EzIPC] public Func<ushort, int> GetRaphaelStatus;
        [EzIPC] public Func<ushort, string> GetRaphaelFailure;

        public void ClearAppliedSettings()
        {
            if (Utils.HasPlugin(Name)) ClearAllTemporarySettings();
            CraftSettings.Clear();
            appliedMission = null;
        }

        private bool AssignArtisanRecipe(uint recipeId, ArtisanSettings recipeConfig, bool isExpert)
        {
            // Extended Artisan deliberately exposes validated overrides, not the newer
            // unchecked Change* IPC family. Reject unsupported overrides explicitly.
            if (recipeConfig.ManualId != 0 || recipeConfig.SquadronManualId != 0
                || recipeConfig.ExpertProfileId != 0 || recipeConfig.SkillUsageAmount != -1
                || recipeConfig.MinStepsForMiracle != -1)
                return false;

            string solver = recipeConfig.SolverOverrideName.Length > 0 ? recipeConfig.SolverOverrideName : recipeConfig.ArtisanSolverType switch
            {
                ArtisanCraftType.Default => "",
                ArtisanCraftType.Standard => "Standard Recipe Solver",
                ArtisanCraftType.ProgressOnly => "Progress Only Solver",
                ArtisanCraftType.Raphael => "Raphael Recipe Solver",
                ArtisanCraftType.Macro => $"Macro: {recipeConfig.MacroName}",
                ArtisanCraftType.Expert => "Expert Recipe Solver",
                _ => ""
            };
            ClearTemporaryRecipeSettings(recipeId);
            if ((solver.Length != 0 && !(GetAvailableSolvers(recipeId)?.Contains(solver) == true
                    && SetTemporarySolver(recipeId, solver)))
                || !SetTemporaryFood(recipeId, recipeConfig.FoodId, recipeConfig.FoodHQ)
                || !SetTemporaryPotion(recipeId, recipeConfig.PotionId, recipeConfig.PotionHQ))
            {
                ClearTemporaryRecipeSettings(recipeId);
                return false;
            }
            return true;
        }

        private static bool ArtisanSettingsEqual(ArtisanSettings a, ArtisanSettings b) =>
            a.UseGlobal == b.UseGlobal &&
            a.FoodId == b.FoodId &&
            a.FoodHQ == b.FoodHQ &&
            a.PotionId == b.PotionId &&
            a.PotionHQ == b.PotionHQ &&
            a.ManualId == b.ManualId &&
            a.SquadronManualId == b.SquadronManualId &&
            a.ArtisanSolverType == b.ArtisanSolverType &&
            a.MacroName == b.MacroName &&
            a.SolverOverrideName == b.SolverOverrideName &&
            a.SkillUsageAmount == b.SkillUsageAmount &&
            a.MinStepsForMiracle == b.MinStepsForMiracle &&
            a.ExpertProfileId == b.ExpertProfileId;

        private Dictionary<ushort, ArtisanSettings> CraftSettings = new();
        private uint? appliedMission;

        public bool CheckArtisanSettings(ushort recipeId, uint missionId, bool isExpert, bool isLeveling = false)
        {
            if (appliedMission != missionId)
            {
                ClearAppliedSettings();
                appliedMission = missionId;
            }
            if (!C.MissionConfig.TryGetValue(missionId, out var configInfo))
                return true;
            if (!configInfo.CraftSettings.TryGetValue(recipeId, out var recipeSettings))
                return true;

            // Resolve the effective settings — global profile or per-recipe config
            ArtisanSettings effectiveSettings;
            if (recipeSettings.UseGlobal)
            {
                var global = isExpert ? C.Artisan_GlobalExpert : C.Artisan_GlobalStandard;
                effectiveSettings = new ArtisanSettings
                {
                    UseGlobal = true,
                    FoodId = global.FoodId,
                    FoodHQ = global.FoodHQ,
                    PotionId = global.PotionId,
                    PotionHQ = global.PotionHQ,
                    ManualId = global.ManualId,
                    SquadronManualId = global.SquadronManual,
                    ArtisanSolverType = global.SolverType,
                    // MacroName, SkillUsageAmount, MinStepsForMiracle, ExpertProfileId
                    // I'm not using these in the global, so they're just getting defaulted
                };
            }
            else
            {
                effectiveSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<ArtisanSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(recipeSettings));
            }

            if (isLeveling)
            {
                effectiveSettings.ArtisanSolverType = ArtisanCraftType.ProgressOnly;
                effectiveSettings.SolverOverrideName = "";
            }

            if (CraftSettings.TryGetValue(recipeId, out var cached) && ArtisanSettingsEqual(cached, effectiveSettings))
                return true;

            if (!AssignArtisanRecipe(recipeId, effectiveSettings, isExpert))
            {
                IceLogging.Error($"Recipe {recipeId}: unsupported or invalid Artisan overrides. ICE has stopped.", "[Artisan Settings]");
                return false;
            }
            CraftSettings[recipeId] = new ArtisanSettings
            {
                UseGlobal = effectiveSettings.UseGlobal,
                FoodId = effectiveSettings.FoodId,
                FoodHQ = effectiveSettings.FoodHQ,
                PotionId = effectiveSettings.PotionId,
                PotionHQ = effectiveSettings.PotionHQ,
                ManualId = effectiveSettings.ManualId,
                SquadronManualId = effectiveSettings.SquadronManualId,
                ArtisanSolverType = effectiveSettings.ArtisanSolverType,
                MacroName = effectiveSettings.MacroName,
                SolverOverrideName = effectiveSettings.SolverOverrideName,
                SkillUsageAmount = effectiveSettings.SkillUsageAmount,
                MinStepsForMiracle = effectiveSettings.MinStepsForMiracle,
                ExpertProfileId = effectiveSettings.ExpertProfileId,
            };
            return true;
        }

        public bool UpdatedArtisan()
        {
            if (DalamudReflector.TryGetDalamudPlugin($"Artisan", out var plogon, false, true))
            {
                if (plogon.GetType().Assembly.GetName().Version < new Version(4, 0, 4, 25))
                    return false;

                return true;
            }

            return false;
        }

        /*
        public List<(string Name, int Id)> MacroList()
        {
            if (ReturnMacroInfo == null)
                return new List<(string Name, int Id)>();

            return ReturnMacroInfo.Invoke();
        }
        */

        public class MacroInfo
        {
            public string Name;
            public int Id;
        }

        public List<MacroInfo> MacroList()
        {
            List<MacroInfo> artisanMacros = new();

            if (DalamudReflector.TryGetDalamudPlugin(Name, out var pluginObj, false, false))
            {
                var config = pluginObj.GetFoP("Config");
                var macroSolverConfig = config.GetFoP("MacroSolverConfig");
                var macrosObj = macroSolverConfig.GetFoP("Macros"); // object, boxed List<Macro>

                if (macrosObj is System.Collections.IList macroList)
                {
                    var result = new List<MacroInfo>();
                    foreach (var macro in macroList)
                    {
                        var id = (int)macro.GetFoP("ID");
                        var name = (string)macro.GetFoP("Name");
                        result.Add(new()
                        {
                            Name = name,
                            Id = id,
                        });
                    }

                    artisanMacros = result;
                }
            }

            return artisanMacros;
        }
    }
}
