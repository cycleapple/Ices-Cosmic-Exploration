using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices.Legacy;
using ECommons.GameHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ICE.Utilities.Cosmic_Helper;
using Pictomancy;
using SharpDX.Direct3D11;
using System;

namespace ICE.Utilities;

/// <summary>
/// Misc unused (yet) or weird functions i didn't know where to put - Chika
/// </summary>
public static unsafe class Utils
{
    public static uint DarkMatter_8Id = 33916;

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    public static TaskManagerConfiguration TaskConfig => new(timeLimitMS: 10 * 60 * 3000, abortOnTimeout: false);

    public static unsafe void SetFlagForNPC(uint territoryId, float x, float y)
    {
        var map = ExcelHelper.TerritorySheet.GetRow(territoryId).Map.Value;

        var agent = AgentMap.Instance();

        Vector2 pos = MapToWorld(new Vector2(x, y), map.SizeFactor, map.OffsetX, map.OffsetY);

        agent->FlagMarkerCount = 0;
        agent->SetFlagMapMarker(territoryId, map.RowId, pos.X, pos.Y);
        agent->OpenMapByMapId(map.RowId, territoryId);
    }

    public static void SetRawFlagforNpc(uint territoryId, float x, float y)
    {
        var map = ExcelHelper.TerritorySheet.GetRow(territoryId).Map.Value;

        var agent = AgentMap.Instance();

        agent->FlagMarkerCount = 0;
        agent->SetFlagMapMarker(territoryId, map.RowId, x, y);
        agent->OpenMapByMapId(map.RowId, territoryId);
    }

    public static float MapToWorld(float value, uint scale, int offset) => -offset * (scale / 100.0f) + 50.0f * (value - 1) * (scale / 100.0f);

    public static Vector2 MapToWorld(Vector2 coordinates, ushort sizeFactor, short offsetX, short offsetY)
    {
        var scalar = sizeFactor / 100.0f;

        var xWorldCoord = MapToWorld(coordinates.X, sizeFactor, offsetX);
        var yWorldCoord = MapToWorld(coordinates.Y, sizeFactor, offsetY);

        var objectPosition = new Vector2(xWorldCoord, yWorldCoord);
        var center = new Vector2(1024.0f, 1024.0f);

        return objectPosition / scalar - center / scalar;
    }

    public static bool? TargetgameObjectTask(IGameObject? gameObject)
    {
        var x = gameObject;
        if (Svc.Targets.Target != null && Svc.Targets.Target.BaseId == x.BaseId)
            return true;

        if (!GenericHelpers.IsOccupied())
        {
            if (x != null)
            {
                if (EzThrottler.Throttle($"Throttle Targeting {x.BaseId}"))
                {
                    Svc.Targets.SetTarget(x);
                    IceLogging.Info($"Setting the target to {x.BaseId}");
                }
            }
        }
        return false;
    }
    internal static bool TryGetObjectByDataId(ulong dataId, out IGameObject? gameObject) => (gameObject = Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.BaseId == dataId)) != null;
    public static IGameObject? TryGetObjectNearestEventObject()
    {
        return Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj);
    }
    public static IGameObject? TryGetObjectCollectionPoint()
    {
        return Svc.Objects.OrderBy(Player.DistanceTo).FirstOrDefault(x => x.BaseId == 2014616 || x.BaseId == 2014618);
    }
    public static void TargetgameObject(IGameObject? gameObject)
    {
        var x = gameObject;

        // porting-note: upstream declares the parameter nullable and guards with `if (x != null)`
        // further down, but dereferences x.BaseId above that guard -- so a null argument throws
        // instead of being ignored. All six call sites feed it `Utils.TryGetObjectByDataId(id, out
        // var o)` while DISCARDING the bool, so whenever the NPC or node is not currently spawned
        // nearby the object is null and this NREs inside per-frame TaskManager work. Hit live on
        // 2026-08-04 via Task_RelicTurnin.TalkToResearchWay; Task_ArtifactSearch, Task_BuyCosmoItems,
        // Task_Gamba, Task_Gather and Task_Repair are the same shape. Guarding the one helper covers
        // all of them, and matches how the sibling InteractWithObject already behaves.
        if (x == null)
        {
            // Returning quietly here is what turned the 2026-08-04 relic-turnin crash into a
            // 2026-08-04 relic-turnin HANG: the task chain reached the NPC and then sat there with
            // nothing in the log. A guard that swallows the reason is only half a fix, so say what
            // was actually nearby -- the id we failed to match is one of these BaseIds.
            if (EzThrottler.Throttle("Target lookup miss", 3000))
            {
                var nearby = Svc.Objects
                    .Where(o => o != null && Player.DistanceTo(o) < 30f)
                    .OrderBy(Player.DistanceTo)
                    .Take(8)
                    .Select(o => $"{o.BaseId}:{o.Name}({o.ObjectKind},{Player.DistanceTo(o):F1}y)");

                IceLogging.Warning(
                    $"asked to target a null object -- the caller's TryGetObjectByDataId found no match. " +
                    $"Nearby: {string.Join(" | ", nearby)}", "[Target Game Object]");
            }
            return;
        }

        var currentTarget = Svc.Targets.Target;
        if (currentTarget != null && currentTarget.DataId == x.BaseId)
            return;

        if (!GenericHelpers.IsOccupied())
        {
            if (x != null)
            {
                if (EzThrottler.Throttle($"Throttle targeting: {x.BaseId}"))
                {
                    IceLogging.Info($"Attempting to set the target to: {x.BaseId} | {x.Name}", "[Target Game Object]");
                    Svc.Targets.SetTarget(x);
                }
            }
        }
    }
    public static unsafe void InteractWithObject(IGameObject? gameObject)
    {
        try
        {
            if (gameObject == null || !gameObject.IsTargetable)
                return;
            var gameObjectPointer = (GameObject*)gameObject.Address;
            TargetSystem.Instance()->InteractWithObject(gameObjectPointer, false);
        }
        catch (Exception ex)
        {
            IceLogging.Error($"InteractWithObject: Exception: {ex}");
        }
    }
    public static unsafe void SetGatheringRing(uint territoryId, int x, int y, int radius, string? tooltip = "Node Location", uint iconId = 60561)
    {
        var map = ExcelHelper.TerritorySheet.GetRow(territoryId).Map.Value;
        var agent = AgentMap.Instance();

        Vector2 pos = MapToWorld(new Vector2(x, y), map.SizeFactor, map.OffsetX, map.OffsetY);
        IceLogging.Debug($"Current map: {map.RowId} {territoryId} | {map.PlaceName.Value.Name} | {pos.X} {pos.Y} | {x} {y} | {radius} | {tooltip}");

        agent->FlagMarkerCount = 0;
        // agent->IsFlagMarkerSet = false;
        agent->SetFlagMapMarker(territoryId, map.RowId, x, y, iconId);
        agent->TempMapMarkerCount = 0;
        agent->AddGatheringTempMarker(x, y, radius, tooltip: tooltip);
        agent->OpenMap(map.RowId, territoryId, tooltip, MapType.GatheringLog);
    }
    public static unsafe void MountAction()
    {
        IceLogging.Verbose("We were told to use a mount action");
        bool useMount = Char_Info.MountId != 0 && PlayerState.Instance()->IsMountUnlocked(Char_Info.MountId);

        if (!Player.IsCasting && !Player.Mounting)
        {
            if (useMount)
            {
                ActionManager.Instance()->UseAction(ActionType.Mount, Char_Info.MountId);
                IceLogging.Info($"Attempting to mount: {Char_Info.MountName}");
            }
            else
            {
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
                IceLogging.Info($"Resorting to using the mount roulette");
            }
        }
    }
    public static unsafe void Dismount()
    {
        if (Player.Mounted)
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
        }
    }

    public static uint ToUintABGR(Vector4 col)
    {
        byte a = (byte)(col.W * 255);
        byte b = (byte)(col.Z * 255);
        byte g = (byte)(col.Y * 255);
        byte r = (byte)(col.X * 255);
        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }

    public static Vector4 FromUintABGR(uint color)
    {
        float a = ((color >> 24) & 0xFF) / 255f;
        float b = ((color >> 16) & 0xFF) / 255f;
        float g = ((color >> 8) & 0xFF) / 255f;
        float r = (color & 0xFF) / 255f;
        return new Vector4(r, g, b, a);
    }

    public static void VnavBuildInfo()
    {
        if (EzThrottler.Throttle("Vnavmesh throttle message", 1000))
            IceLogging.Debug($"Navmesh isn't ready. % built is at: {P.Navmesh.BuildProgress}");
    }
}
