using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ICE.ConfigFiles;
using ICE.IPC;
using ICE.Ui;
using ICE.Utilities.Cosmic_Helper;
using Pictomancy;
using System.Collections.Generic;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using ICE.Scheduler.Handlers.PictoStuff;
using ICE.Utilities.GatheringHelper.RouteLoader;

namespace ICE;

public sealed partial class ICE : IDalamudPlugin
{
    public static string Name => "ICE Candidate";
    internal static bool StableVersionLoaded => Svc.PluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == CandidateIdentity.StableInternalName && plugin.IsLoaded);

    internal static ICE P = null!;
    private Config config;
    internal static string LegacyImportError { get; set; } = "";
    public static Config C => P.config;
    // porting-note(api13): the TC tree vendors the pre-rename Pictomancy, where PictoService is a
    // static class rather than a PctContext instance returned by Initialize. The field is dropped
    // and every `PictoService.X` call site is sed'd to `PictoService.X` (same shape AutoDuty/Splatoon use).

    // Missing ECommons PluginService. Update to Svc when ECommons get updated
    public static TcUnlockState UnlockState { get; } = new();
    
    public MissionTimer MissionTimer { get; private set; }

    // Window's that I use, base window to the settings... need these to actually show shit 
    internal WindowSystem windowSystem;
    internal MainWindow mainWindow;
    internal OverlayWindow overlayWindow;
    internal DebugWindow debugWindow;
    internal InfoWindow infoWindow;
    internal Window_ExternalDetails externalDetails;

    // Taskmanager from Ecommons
    internal TaskManager TaskManager;

    // Internal IPC's that I use for... well plugins. 
    internal LifestreamIPC Lifestream;
    internal NavmeshIPC Navmesh;
    internal PandoraIPC Pandora;
    internal ArtisanIPC Artisan;
    internal VislandIPC Visland;
    internal AutoHookIPC AutoHook;
    internal IceCosmicExplorationIPC IceIpc;
    internal GlamourerIPC GlamourIpc;

    public ICE(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P, Module.DalamudReflector, ECommons.Module.ObjectFunctions);
        new ECommons.Schedulers.TickScheduler(Load);
        PictoService.Initialize(pi);
    }
    public void Load()
    {
        // Candidate schema differs from 0.0.73.x. Never rewrite the stable configuration.
        EzConfig.PluginConfigDirectoryOverride = CandidateIdentity.ConfigDirectory;
        try
        {
            LegacyConfigImporter.TryImport(
                CandidateIdentity.LegacyConfigPath(Svc.PluginInterface.GetPluginConfigDirectory()),
                EzConfig.DefaultConfigurationFileName);
        }
        catch (Exception error)
        {
            // Stop initialization rather than silently running with different settings.
            Svc.Log.Error(error, "舊版 ICE 設定匯入失敗；原始檔未變更。請查看紀錄並重新確認設定。");
            LegacyImportError = error.Message;
        }
        EzConfig.Migrate<Config>();
        config = EzConfig.Init<Config>();
        if (LegacyImportError.Length != 0)
        {
            config.LegacyImportFailure = LegacyImportError;
            config.Save();
        }
        else LegacyImportError = config.LegacyImportFailure;

        //IPC's that are used
        Lifestream = new();
        Navmesh = new();
        Pandora = new();
        Artisan = new();
        Visland = new();
        AutoHook = new();
        IceIpc = new();
        GlamourIpc = new(Svc.PluginInterface);

        // all the windows
        windowSystem = new();
        mainWindow = new();
        overlayWindow = new();
        debugWindow = new();
        infoWindow = new();
        externalDetails = new();

        EzCmd.Add(CandidateIdentity.Command, OnCommand, "開啟 ICE 新版測試版；使用 /icecandidate help 查看指令。");
        Init();
        Svc.Framework.Update += Tick;
        Svc.PluginInterface.UiBuilder.Draw += OnDraw;

        TaskManager = new(new(showDebug: false, timeLimitMS: 10 * 60 * 3000));
        Svc.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += () =>
        {
            mainWindow.IsOpen = true;
        };
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () =>
        {
            mainWindow.IsOpen = true;
            C.SelectedTab = WindowSelection.MiscSettings;
        };

        Svc.ClientState.TerritoryChanged += OnTerritoryChange;

        // timer stuff
        MissionTimer = new MissionTimer();

        DictionaryCreation();
        Task_Gamba.EnsureGambaWeightsInitialized();
        CosmicHelper.UpdateCriticalWeather();
        TestLoadRoutes();
        CosmicHelper.Task_UpdateRelicMissionInfo();
        GatheringRouteLoader.LoadAllRoutes();

        MigrateConfigSettings();
        _ = Sounds.SoundPlayer.InitializeAsync();

        UpdateMissingGathering();
    }

    private static void Init()
    {
        ExcelHelper.Init();
        ConsumableInfo.Init();
    }

    internal void ApplyImportedSettings(string json)
    {
        SchedulerMain.DisablePlugin();
        config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json)
            ?? throw new InvalidOperationException("匯入設定無效。");
        EzConfig.Set(config);
        LegacyImportError = "";
        EnsureAllMission();
        MigrateConfigSettings();
        UpdateMissingGathering();
        global::ICE.Ui.MainUi.ModeSelect_Modes.Mission_Setup.MissionTable = null;
        config.Save();
    }

    private void Tick(object _)
    {
        if (StableVersionLoaded)
        {
            if (SchedulerMain.State != IceState.Idle) SchedulerMain.DisablePlugin();
            return;
        }
        if (Player.Available && PlayerHelper.IsInCosmicZone())
        {
            if (EzThrottler.Throttle("Update Character Stats"))
            {
                CosmicHelper.Task_UpdateRelicMissionInfo();
            }
            PlayerHandlers.Tick();
            if (SchedulerMain.State != IceState.Idle)
                SchedulerMain.Tick();
            WeatherForecastHandler.Tick();


        }
        else if (!Player.Available)
        {
            if (SchedulerMain.State != IceState.Idle)
                PlayerHandlers.DisablePlugin();
            if (PlayerHandlers.PlayerFirstCosmicZone)
                PlayerHandlers.PlayerFirstCosmicZone = false;
        }

        if (PlayerHelper.IsInCosmicZone())
        {
            GenericManager.Tick();
            TextAdvancedManager.Tick();
            YesAlreadyManager.Tick();
        }
        else
        {
            if (SchedulerMain.State != IceState.Idle)
                SchedulerMain.DisablePlugin();
        }

        if (SchedulerMain.State == IceState.Idle && !GenericManager.Pandora_WasRestored)
        {
            if (EzThrottler.Throttle("Turning on pandora features"))
                GenericManager.RestorePandoraStates();
        }
    }

    // api13: IClientState.TerritoryChanged is Action<ushort>, not Action<uint> (api14+).
    private void OnTerritoryChange(ushort TerritoryId)
    {
        bool isInCosmic = PlayerHelper.IsInCosmicZone();
        if (isInCosmic && !overlayWindow.IsOpen && C.ShowOverlay)
            P.overlayWindow.IsOpen = true;
        else if (!isInCosmic && overlayWindow.IsOpen)
            P.overlayWindow.IsOpen = false;
    }

    private void OnDraw()
    {
        if (PlayerHelper.IsInCosmicZone() && Player.Available)
        {
            PictoManager.DrawPicto();
        }
    }

    public void Dispose()
    {
        GenericHelpers.Safe(Artisan.ClearAppliedSettings);
        GenericHelpers.Safe(() => Svc.Framework.Update -= Tick);
        GenericHelpers.Safe(() => Svc.PluginInterface.UiBuilder.Draw -= OnDraw);
        GenericHelpers.Safe(() => Svc.PluginInterface.UiBuilder.Draw -= windowSystem.Draw);
        GenericHelpers.Safe(() => Svc.ClientState.TerritoryChanged -= OnTerritoryChange);
        GenericHelpers.Safe(TextAdvancedManager.UnlockTA);
        GenericHelpers.Safe(YesAlreadyManager.Unlock);
        GenericHelpers.Safe(PictoService.Dispose);
        ECommonsMain.Dispose();
        PictoService.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var subcommands = args.Split(' ');

        if (subcommands.Length == 0 || args == "")
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
            return;
        }

        var firstArg = subcommands[0];

        if (firstArg.ToLower() == "d" || firstArg.ToLower() == "debug")
        {
            debugWindow.IsOpen = true;
            return;
        }
        else if (firstArg.ToLower() == "i")
        {
            infoWindow.IsOpen = true;
            return;
        }
        else if (firstArg.ToLower() == "s" || firstArg.ToLower() == "settings")
        {
            mainWindow.IsOpen = true;
            C.SelectedTab = WindowSelection.MiscSettings;
            return;
        }
        else if (firstArg.ToLower() == "clear")
        {
            foreach (var mission in C.MissionConfig)
            {
                mission.Value.Enabled = false;
            }
            C.Save();
        }
        else if (firstArg.ToLower() == "stop")
        {
            SchedulerMain.DisablePlugin();
        }
        else if (firstArg.ToLower() == "start")
        {
            SchedulerMain.EnablePlugin();
        }
        else if (firstArg.ToLower() == "add")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            foreach (var id in idSet)
            {
                if (C.MissionConfig.TryGetValue(id, out var mission))
                {
                    mission.Enabled = true;
                }
            }
            C.Save();
        }
        else if (firstArg.ToLower() == "remove")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            foreach (var id in idSet)
            {
                if (C.MissionConfig.TryGetValue(id, out var mission))
                {
                    mission.Enabled = false;
                }
            }
            C.Save();
        }
        else if (firstArg.ToLower() == "toggle")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            foreach (var id in idSet)
            {
                if (C.MissionConfig.TryGetValue(id, out var mission))
                {
                    mission.Enabled = !mission.Enabled;
                }
            }
            C.Save();
        }
        else if (firstArg.ToLower() == "only")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            foreach (var mission in C.MissionConfig.Where(x => x.Value.Enabled))
            {
                mission.Value.Enabled = false;
            }
            foreach (var id in idSet)
            {
                if (C.MissionConfig.TryGetValue(id, out var mission))
                {
                    mission.Enabled = true;
                }
            }
        }
        else if (firstArg.ToLower() == "flag")
        {
            if (subcommands.Length != 2) return;
            if (!PlayerHelper.IsInCosmicZone()) return;

            int missionId = int.Parse(subcommands[1]);
            var info = CosmicHelper.SheetMissionDict.FirstOrDefault(mission => mission.Key == missionId);
            if (info.Value == default) return;
            if (info.Value.MarkerId == 0) return;

            Utils.SetGatheringRing(info.Value.TerritoryId, (int)info.Value.MapPosition.X, (int)info.Value.MapPosition.Y, info.Value.Radius, info.Value.Name);
        }
        else if (firstArg.ToLower() == "help")
        {
            string helpMessage = $"- - ICE Commands Help - - \n" +
                                 $"/icecandidate help - show all available commands\n" +
                                 $"/icecandidate -> opens the main settings\n" +
                                 $"/icecandidate s -> opens the settings menu\n" +
                                 $" - - - Mission specific - - - \n" +
                                 $"/icecandidate stop - Stops ICE\n" +
                                 $"/icecandidate start - starts ICE \n" +
                                 $"The rest of the commands work by doing a single id/multiple in a row \n" +
                                 $"EX. /icecandidate add 10 155 185\n" +
                                 $"/icecandidate add (ids) - enables select missions\n" +
                                 $"/icecandidate remove (ids) - removes/disables select missions\n" +
                                 $"/icecandidate toggle (ids) - toggles select mission ids" +
                                 $"/icecandidate only (ids) - makes only select missions enabled" +
                                 $"/icecandidate flag (id) - opens the map and flags the mission (if it has one).\n";
            Svc.Chat.Print(helpMessage);
        }
        else if (firstArg.ToLower() == "overlay")
        {
            overlayWindow.IsOpen = false;
            overlayWindow.IsOpen = true;
        }
    }

    public void TestLoadRoutes()
    {

    }
}
