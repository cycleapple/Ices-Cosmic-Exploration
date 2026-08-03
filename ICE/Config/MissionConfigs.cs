using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ICE.Config
{
    public class MissionConfigs : IYamlConfig
    {
        // Last edited version: 1
        public int ConfigVersion { get; set; } = 13;

        #region Safety Settings
        public bool StopOnAbort { get; set; } = true;
        public bool DelayGrabMission { get; set; } = true;
        public int DelayIncrease { get; set; } = 500;
        public bool DelayCraft { get; set; } = true;
        public int DelayCraftIncrease { get; set; } = 2500;
        public bool AnimationLockAbandon { get; set; } = true;
        public bool JumpIfStuck { get; set; } = false;

        #endregion

        #region Main Window

        public bool ShowInfoButton { get; set; } = true;
        public float MiddleColumnWidth { get; set; } = 1000f;
        public uint SelectedJob { get; set; } = 8;
        public bool XPRelicGrind { get; set; } = false;
        public bool XPRelicIgnoreManual { get; set; } = false;
        public bool XPRelicOnlyEnabled { get; set; } = false;
        public bool CosmicAgendaMode { get; set; } = false;
        public List<AgendaEntry> CosmicAgenda { get; set; } = new();
        public bool ShowCritical { get; set; } = true;
        public bool ShowSequential { get; set; } = true;
        public bool ShowWeather { get; set; } = true;
        public bool ShowTimeRestricted { get; set; } = true;
        public bool ShowClassA { get; set; } = true;
        public bool ShowClassB { get; set; } = true;
        public bool ShowClassC { get; set; } = true;
        public bool ShowClassD { get; set; } = true;
        public Dictionary<string, bool> MissionDisplayCategories { get; set; } = new();

        #endregion

        #region Overlay Settings

        public bool ShowOverlay { get; set; } = false;
        public bool ShowSeconds { get; set; } = false;
        public bool ShowTotalScore { get; set; } = true;
        public bool ShowExpBars { get; set; } = true;

        #endregion

        #region MissionSettings

        public bool OnlyGrabMission { get; set; } = false;
        public int TargetLevel { get; set; } = 10;
        public bool StopWhenLevel { get; set; } = false;
        public bool StopOnceHitCosmoCredits { get; set; } = false;
        public int CosmoCreditsCap { get; set; } = 30000;
        public bool StopOnceHitLunarCredits { get; set; } = false;
        public int LunarCreditsCap { get; set; } = 10000;
        public bool StopOnceHitCosmicScore { get; set; } = false;
        public int CosmicScoreCap { get; set; } = 500000;
        public bool StopOnceRelicFinished { get; set; } = false;
        public bool StopOnceStandardMissionsGolded { get; set; } = false;
        public bool StopStandardGoldARank { get; set; } = true;
        public bool StopStandardGoldBRank { get; set; } = true;
        public bool StopStandardGoldCRank { get; set; } = true;
        public bool StopStandardGoldDRank { get; set; } = true;
        public byte SequenceMissionPriority { get; set; } = 1;
        public byte WeatherMissionPriority { get; set; } = 2;
        public byte TimedMissionPriority { get; set; } = 3;
        public List<ProvisionalTypes> MissionPrio { get; set; } = new()
        {
            ProvisionalTypes.ProvisionalWeather,
            ProvisionalTypes.ProvisionalSequential,
            ProvisionalTypes.ProvisionalTimed
        };
        public bool GrindProvisionals { get; set; } = false;
        public List<uint> JobPrio { get; set; } = new()
        {
            8, 9, 10, 11, 12, 13, 14, 15,  // Crafters: CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL
            16, 17, 18                     // Gatherers: MIN, BTN, FSH
        };
        public bool AutoSelectMoon { get; set; } = true;
        public bool ShowSinusMissions { get; set; } = true;
        public bool ShowPhaennaMissions { get; set; } = true;
        public bool RemoveAfterGold { get; set; } = false;
        public bool RemoveAfterThreeGoldFailures { get; set; } = false;
        public bool SkipHubActivitiesDuringRedAlert { get; set; } = false;
        public bool ShowExtraMissionInfo { get; set; } = true;
        public Dictionary<uint, uint> ScoreKeeper { get; set; } = new();

        #endregion

        #region Table Settings

        public int TableSortOption { get; set; } = 0;
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public bool ShowCompletionWindow { get; set; } = false;
        public bool ShowCompletionOnlyJob { get; set; } = false;
        public bool ShowSelectedJobOnly { get; set; } = false;
        public bool ShowCompletion_MissingGold { get; set; } = false;
        public bool ShowManualMode { get; set; } = false;
        public bool Auto_ShowTokens { get; set; } = true;

        #endregion

        #region Repair Settings

        public bool SelfRepairGather { get; set; } = true;
        public bool SelfRepairCrafter { get; set; } = false;
        public bool RepairAtVendor { get; set; } = false;
        public int RepairPercent { get; set; } = 50;
        public bool SelfSpiritbondGather { get; set; } = true;

        #endregion

        #region Gathering Settings
        public int SelectedGatherIndex { get; set; } = 0;
        public bool UseGatheringFood { get; set; } = false;
        public uint GatheringFood { get; set; } = 0;

        #region Cordial Settings

        public bool AutoCordial { get; set; } = false;
        public bool inverseCordialPrio { get; set; } = false;
        public int CordialMinGp { get; set; } = 0;
        public bool UseOnFisher { get; set; } = false;
        public bool PreventOvercap { get; set; } = false;
        public bool UseOnlyInMission { get; set; } = false;

        #endregion

        public List<GatherProfile> GatherSettings { get; set; } = new()
        {
            new GatherProfile { Id = 0, Name = "Defualt"},
        };

        public Dictionary<int, GatherProfile> GatherProfiles { get; set; } = new()
        {
            [0] = new GatherProfile() 
            { 
                Name = "Default",
            },
        };

        public int SelectedCraftProfileId { get; set; } = 0;
        public Dictionary<int, CraftProfile> CraftProfiles { get; set; } = new()
        {
            [0] = new CraftProfile { Id = 0, Name = "預設" },
        };

        #endregion

        #region Gamba Settings

        // Gamba settings
        public List<Gamba> GambaItemWeights { get; set; } = new();
        public bool GambaEnabled { get; set; } = false;
        public bool GambaPreferSmallerWheel { get; set; } = false;
        public int GambaCreditsMinimum { get; set; } = 0;
        public int GambaDelay { get; set; } = 250;
        public bool GambaBetweenRuns = false;
        public int GambaAtAmount { get; set; } = 1000;

        #endregion

        #region Misc

        public bool MoonSprint { get; set; } = true;
        public uint MountId { get; set; } = 0;
        public string MountName { get; set; } = "Mount Roulette";
        public float MountRadius { get; set; } = 15.0f;
        public float DismountRadius { get; set; } = 7.0f;
        public bool UseMountOutsideMission { get; set; } = true;
        public bool UseMountInMission { get; set; } = true;
        public bool UseHubReturn { get; set; } = true;
        public bool UseRedAlertNpc { get; set; } = false;
        public float LeftColumnWidth { get; set; } = 300f;
        public bool PlaySoundAlert { get; set; } = false;
        public float SoundVolume { get; set; } = 0.5f;
        public int TimeHistoryLimit { get; set; } = 100;
        public bool RemoveStellarStatus { get; set; } = false;
        public bool ShowSPM { get; set; } = false;

        #endregion

        #region Relic Settings
        
        public bool TurninRelic { get; set; } = false;
        public Dictionary<uint, bool> ClassesUnlocked { get; set; } = new()
        {
            [8] = true,
            [9] = true,
            [10] = true,
            [11] = true,
            [12] = true,
            [13] = true,
            [14] = true,
            [15] = true,
            [16] = true,
            [17] = true,
            [18] = true
        };

        #endregion

        #region Shopping List

        public Dictionary<uint, CosmoShoppingList> CosmoShopping { get; set; } = new();
        public List<uint> CosmoShoppingOrder { get; set; } = new();
        public bool BuyItems { get; set; } = false;
        public int CosmoBuyAtAmount { get; set; } = 10000;

        #endregion

        public Dictionary<uint, MissionSettings> MissionConfig { get; set; } = new();

        public Dictionary<string, List<uint>> MissionPlaylists { get; set; } = new();

        public List<MissionCommand> PostMissionCommands { get; set; } = new();

        #region Tab Hider

        public bool Show_StopWhen { get; set; } = true;
        public bool Show_GatheringProfile { get; set; } = true;
        public bool Show_MissionPriority { get; set; } = true;
        public bool Show_MiscSettings { get; set; } = true;
        public bool Show_HubActivities { get; set; } = true;

        #endregion

        #region Debug

        public bool FailsafeRecipeSelect { get; set; } = false;
        public bool UseDummyXp { get; set; } = false;
        public Dictionary<int, CosmicHelper.XPType> DummyXP { get; set; } = new()
        {
            { 1, new CosmicHelper.XPType { CurrentXP = 0, NeededXP = 100} },
            { 2, new CosmicHelper.XPType { CurrentXP = 50, NeededXP = 200} },
            { 3, new CosmicHelper.XPType { CurrentXP = 100, NeededXP = 300} },
            { 4, new CosmicHelper.XPType { CurrentXP = 150, NeededXP = 400} },
            { 5, new CosmicHelper.XPType { CurrentXP = 200, NeededXP = 500} },
        };
        public uint PictoColor_Circle { get; set; } = 2616716297;
        public uint PictoColor_Dot { get; set; } = 2616716297;
        public uint PictoColor_Cone { get; set; } = 0;
        public bool UseDummyRanks { get; set; } = false;
        public bool ShowDummyA { get; set; } = false;
        public bool ShowDummyB { get; set; } = false;
        public bool ShowDummyC { get; set; } = false;
        public bool ShowDummyD { get; set; } = false;

        public bool DisablePathfindingToRedAlert { get; set; } = false;
        public bool ShowDebugGatherInfo { get; set; } = false;
        public string AuthorName { get; set; } = "Puni.sh Community";
        public string CustomRoutePath { get; set; } = string.Empty;


        #endregion

        #region Yaml Save Stuff

        public static string ConfigPath => Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Mission Config.yaml");
        private static CancellationTokenSource? _saveCts;
        private static readonly object _saveLock = new();
        private static readonly SemaphoreSlim _saveGate = new(1, 1);

        // Standard async save (fire-and-forget)
        public void Save()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SaveAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Failed to save MissionConfigs: {ex}");
                }
            });
        }

        // Debounced save for rapid operations
        public void SaveDebounced(int delayMs = 500)
        {
            lock (_saveLock)
            {
                _saveCts?.Cancel();
                _saveCts = new CancellationTokenSource();
                var cts = _saveCts;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delayMs, cts.Token);
                        await SaveAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Newer save cancelled this one
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Failed to save MissionConfigs: {ex}");
                    }
                });
            }
        }

        // Core async implementation
        public async Task SaveAsync()
        {
            await _saveGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await YamlConfig.SaveAsync(this, ConfigPath).ConfigureAwait(false);
            }
            finally
            {
                _saveGate.Release();
            }
        }

        // Synchronous for migrations/critical paths
        public void SaveSync()
        {
            _saveGate.Wait();
            try
            {
                YamlConfig.SaveSync(this, ConfigPath);
            }
            finally
            {
                _saveGate.Release();
            }
        }

        #endregion
    }

    public enum AgendaGoal
    {
        RelicStage,
        CosmoCredits,
        ClassLevel,
        ClassScore,
        StandardMissionsGolded,
        LunarCredits,
    }

    public class AgendaEntry
    {
        public AgendaGoal Goal { get; set; } = AgendaGoal.RelicStage;
        public uint Job { get; set; } = 8;
        public int Target { get; set; } = 1;
        public bool? StandardGoldARank { get; set; }
        public bool? StandardGoldBRank { get; set; }
        public bool? StandardGoldCRank { get; set; }
        public bool? StandardGoldDRank { get; set; }
    }

    public class MissionSettings
    {
        public bool Enabled { get; set; } = false;
        public bool ManualMode { get; set; } = false;
        public int GatherProfileId { get; set; } = 0;
        public int GProfileId { get; set; } = 0;
        public bool AutoTurnin { get; set; } = true;
        public bool TurninGold { get; set; } = false;
        public bool TurninSilver { get; set; } = false;
        public bool TurninBronze { get; set; } = false;
        public bool Use_BuildinPreset { get; set; } = false;
        public string AutoHookPresetName { get; set; } = string.Empty;
        public double BestTime { get; set; } = double.MaxValue;
        public double AverageTime { get; set; } = 0;
        public double AverageBronzeTime { get; set; } = 0;
        public double AverageSilverTime { get; set; } = 0;
        public double AverageGoldTime { get; set; } = 0;
        public double AverageCriticalTime { get; set; } = 0;
        public int TotalCompletions { get; set; } = 0;
        public int BronzeCompletion { get; set; } = 0;
        public int SilverCompletions { get; set; } = 0;
        public int GoldCompletions { get; set; } = 0;
        public int CriticalCompletions { get; set; } = 0;
        public int FailedCounters { get; set; } = 0;
        public Dictionary<ushort, ArtisanRecipeSettings> CraftSettings { get; set; } = new();
        public List<TurninData> TurninRecords { get; set; } = new();
        // Old References to time below for migration
        [YamlIgnore]
        public List<double> Times { get; set; } = new();
    }

    public class TurninData
    {
        public double Time { get; set; }
        public TurninState State { get; set; }
    }

    public class GatherProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int MinimumGp { get; set; } = -1;
        public int DualClassCraftAmount { get; set; } = 1;
        public GatherBuffs GatherBuffs { get; set; } = new();
    }

    public class ArtisanRecipeSettings
    {
        public string SolverName { get; set; } = "";
        public int CraftProfileId { get; set; } = -1;
        public uint FoodId { get; set; }
        public bool FoodHq { get; set; } = true;
        public uint PotionId { get; set; }
        public bool PotionHq { get; set; } = true;
    }

    public class CraftProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ArtisanRecipeSettings Settings { get; set; } = new();
    }

    public class GatherBuff
    {
        public bool Enabled { get; set; } = false;
        public int MinGp { get; set; }
        public int MaxUse { get; set; } = -1;
    }

    public class GatherBuffs
    {
        public Dictionary<string, GatherBuff> Buffs { get; set; } = new()
        {
            ["BoonIncrease2"] = new() { MinGp = 100 },
            ["BoonIncrease1"] = new() { MinGp = 50 },
            ["Tidings"] = new() { MinGp = 200 },
            ["YieldII"] = new() { MinGp = 500 },
            ["YieldI"] = new() { MinGp = 400 },
            ["BountifulYieldII"] = new() { MinGp = 100 },
            ["BonusIntegrity"] = new() { MinGp = 300 },
            ["BonusIntegrityChance"] = new() { Enabled = true, MinGp = 0 },
            ["FieldMasteryIII"] = new() { MinGp = 250 },
            ["FieldMasteryII"] = new() { MinGp = 100 },
            ["FieldMasteryI"] = new() { MinGp = 50 },
            ["FieldMasteryTemp"] = new() { MinGp = 50},
        };

        public int BountifulMinItem { get; set; } = 4;
    }

    public class Gamba
    {
        public uint ItemId { get; set; }
        public int Weight { get; set; } = 0;
        public GambaType Type { get; set; }
    }

    public class CosmoShoppingList
    {
        public int KeepAmount { get; set; } = 0;
        public int BuyAmount { get; set; } = 0;
        public bool KeepBuying { get; set; } = false;
    }

    public class MissionCommand
    {
        public required string command { get; set; }
        public int Delay { get; set; } = 0;
    }
}
