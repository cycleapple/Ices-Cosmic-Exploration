using ICE.Utilities.Cosmic_Helper;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.GatheringHelper;
using System;
using System.Linq;

namespace ICE.Utilities;

// Same game command and API13 signature used by the local AutoHook BaitManager.
// Cosmic bait is equipped by ICE even when MF has disabled AutoHook.
internal sealed class CosmicFishingBait
{
    private delegate byte ExecuteCommandDelegate(int id, int kind, uint itemId, int arg3, int arg4);
    [Signature("E8 ?? ?? ?? ?? 41 C6 04 24")]
    private readonly ExecuteCommandDelegate execute = null!;
    private static CosmicFishingBait instance;
    private CosmicFishingBait() => Svc.Hook.InitializeFromAttributes(this);

    internal static unsafe bool EnsureEquipped()
    {
        var manager = WKSManager.Instance();
        if (manager == null || manager->CurrentMissionUnitRowId == 0) return false;
        var available = GatheringUtil.MoonBaits.Values.SelectMany(x => x)
            .Where(id => PlayerHelper.GetItemCount(id, out var count) && count > 0).Distinct().ToArray();
        var bait = FishingBaitPolicy.Select(manager->FishingBait, available);
        if (bait == 0) return false;
        if (manager->FishingBait == bait) return true;
        if (EzThrottler.Throttle("Equip cosmic fishing bait", 1000))
        {
            try
            {
                instance ??= new CosmicFishingBait();
                var result = instance.execute(701, 4, bait, 0, 0);
                IceLogging.Debug($"Equip bait {bait}: command={result}, current={manager->FishingBait}", "Fishing Bait");
            }
            catch (Exception ex)
            {
                if (EzThrottler.Throttle("Cosmic bait failure", 5000))
                    IceLogging.Error($"無法自動選擇釣餌，請手動選餌。{ex.Message}", "Fishing Bait");
            }
        }
        // Wait for game confirmation before casting; a sent command is not success.
        return manager->FishingBait == bait;
    }
}
