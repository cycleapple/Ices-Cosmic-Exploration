using ECommons.EzIpcManager;

using ICE.Config;
using ICE.Utilities.Cosmic_Helper;

namespace ICE.IPC
{
    public class ArtisanIPC
    {
        public const string Name = "Artisan";
        public ArtisanIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);
        public bool Installed => Utils.HasPlugin(Name);

        [EzIPC] public Func<bool> IsBusy;
        [EzIPC] public Func<bool> GetEnduranceStatus;
        [EzIPC] public Action<bool> SetEnduranceStatus;
        [EzIPC] public Func<bool> IsListRunning;
        [EzIPC] public Func<bool> IsListPaused;
        [EzIPC] public Action<bool> SetListPause;
        [EzIPC] public Func<bool> GetStopRequest;
        [EzIPC] public Action<bool> SetStopRequest;
        [EzIPC] public Action<ushort, int> CraftItem;
        [EzIPC] public Func<ushort, int> GetRaphaelStatus;
        [EzIPC] public Func<ushort, string> GetRaphaelFailure;
        [EzIPC] public Action<ushort, uint, uint, uint, uint> AssignRecipie;
        [EzIPC] public Func<uint, string, bool> SetTemporarySolver;
        [EzIPC] public Func<uint, uint, bool, bool> SetTemporaryFood;
        [EzIPC] public Func<uint, uint, bool, bool> SetTemporaryPotion;
        [EzIPC] public Action<uint> ClearTemporaryRecipeSettings;
        [EzIPC] public Action ClearAllTemporarySettings;
        [EzIPC] public Func<uint, string[]> GetAvailableSolvers;
        [EzIPC] public Func<bool, uint[]> GetAvailableFood;
        [EzIPC] public Func<bool, uint[]> GetAvailablePots;

        public void AssignArtisanRecipe(ushort recipeId, uint reqFood, uint reqPotion = 0, uint reqManual = 0, uint reqSquadronManual = 0)
        {
            P.Artisan.AssignRecipie(recipeId, reqFood, reqPotion, reqManual, reqSquadronManual);
        }

        public bool ApplyMissionCraftSettings(uint missionId)
        {
            ClearAppliedMissionSettings();
            if (!CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var mission)
                || !C.MissionConfig.TryGetValue(missionId, out var missionConfig))
                return true;

            missionConfig.CraftSettings ??= new();
            var hasConfiguredOverrides = missionConfig.CraftSettings.Values.Any(HasEffectiveOverride);
            if (!Installed)
            {
                if (hasConfiguredOverrides)
                    IceLogging.Error("此任務已設定 Artisan 製作設定檔，但 Artisan 未安裝或未載入。", "[Artisan Settings]");
                return !hasConfiguredOverrides;
            }

            foreach (var recipeId in mission.Crafts_Main.Keys.Concat(mission.Crafts_Pre.Keys).Distinct())
            {
                if (!missionConfig.CraftSettings.TryGetValue(recipeId, out var settings))
                    continue;
                settings = Resolve(settings);
                if (!HasOverride(settings))
                    continue;

                ClearTemporaryRecipeSettings(recipeId);
                if (settings.SolverName.Length > 0 && !SetTemporarySolver(recipeId, settings.SolverName))
                {
                    ClearAppliedMissionSettings();
                    IceLogging.Error($"配方 {recipeId} 不支援設定的求解器「{settings.SolverName}」，ICE 已停止。", "[Artisan Settings]");
                    return false;
                }
                if (!SetTemporaryFood(recipeId, settings.FoodId, settings.FoodHq)
                    || !SetTemporaryPotion(recipeId, settings.PotionId, settings.PotionHq))
                {
                    ClearAppliedMissionSettings();
                    IceLogging.Error($"配方 {recipeId} 的食物或藥水設定無效，ICE 已停止。", "[Artisan Settings]");
                    return false;
                }
            }

            return true;
        }

        public void ClearAppliedMissionSettings()
        {
            if (Installed)
                ClearAllTemporarySettings();
        }

        private static bool HasEffectiveOverride(ArtisanRecipeSettings settings) => HasOverride(Resolve(settings));

        private static ArtisanRecipeSettings Resolve(ArtisanRecipeSettings settings)
        {
            if (settings.CraftProfileId >= 0
                && C.CraftProfiles.TryGetValue(settings.CraftProfileId, out var profile)
                && profile.Settings != null)
                return profile.Settings;
            return settings;
        }

        private static bool HasOverride(ArtisanRecipeSettings settings)
            => !string.IsNullOrWhiteSpace(settings.SolverName)
               || settings.FoodId != 0
               || settings.PotionId != 0;
    }
}
