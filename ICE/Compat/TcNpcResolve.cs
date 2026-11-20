// =====================================================================================
// porting-note(TC game 7.20): TC-only file. NOT from upstream ICE.
//
// Upstream resolves the Cosmic hub NPCs by ENpcResident id:
//
//     Utils.TryGetObjectByDataId(npcEntry.NpcId, out var npc);   // x.DataId == dataId
//
// Those ids do not match on the TC client. Measured live at the Sinus Ardorum hub on
// 2026-08-04: upstream records Researchingway as 1052605, and the object standing at
// exactly the recorded coordinates has BaseId 1052586. The other EventNpcs in range were
// 1052621 and 1052870 -- so none of upstream's five ids for territory 1237 (1052605 /
// 1052608 / 1052610 / 1052612 / 1052663) matched anything nearby, not just the relic one.
//
// The POSITIONS in NpcData.MoonNpcs are correct on TC -- Relic_PathTo walks to them and
// arrives -- so position is the reliable key here and the id is not. This resolver keeps
// the id as the fast path and falls back to "the EventNpc standing at the recorded spot".
//
// Why not simply correct the five ids: only Researchingway's true value has been observed;
// harvesting the rest would need a trip to each NPC. The fallback fixes all of them at once
// and self-heals if TC and international diverge again. Each fallback hit logs the real
// BaseId, so the table can be corrected later from ordinary use rather than a special trip.
//
// The radius is deliberately tight. Two hub NPCs were 6.4y apart in the measured sample, so
// 3y cannot select a neighbour, and anything outside it means the record itself is wrong --
// in which case failing (and saying so) beats interacting with the wrong NPC.
// =====================================================================================

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ICE.Utilities;
using ICE.Utilities.Cosmic_Helper;

namespace ICE.Compat;

internal static class TcNpc
{
    /// <summary>Max distance from a recorded NPC position that still counts as "that NPC".</summary>
    private const float PositionMatchRadius = 3f;

    /// <summary>
    /// Resolves a hub NPC to its live game object. Prefers the recorded ENpcResident id; if that
    /// matches nothing (the TC case), falls back to the EventNpc standing at the recorded position.
    /// </summary>
    public static bool TryResolve(NpcData.NPCInfo npc, out IGameObject? gameObject)
    {
        gameObject = null;
        if (npc == null)
            return false;

        if (Utils.TryGetObjectByDataId(npc.NpcId, out var byId) && byId != null)
        {
            gameObject = byId;
            return true;
        }

        IGameObject? best = null;
        var bestDistance = PositionMatchRadius;

        foreach (var candidate in Svc.Objects)
        {
            if (candidate == null || candidate.ObjectKind != ObjectKind.EventNpc)
                continue;

            var distance = Vector3.Distance(candidate.Position, npc.Location_Npc);
            if (distance >= bestDistance)
                continue;

            best = candidate;
            bestDistance = distance;
        }

        if (best == null)
        {
            // Nothing at the recorded spot either. Two hypotheses fit "walks there, cannot
            // interact", and the distances below separate them:
            //   * id-only drift        -> some EventNpc sits within a yalm or two of the record
            //     (then widen the radius, or just adopt its BaseId)
            //   * the NPC has MOVED    -> the nearest EventNpc is far off, which is what you would
            //     expect if hub layout tracks Cosmic Exploration's development stage and TC has
            //     not reached the stage upstream's coordinates were captured at. Then the POSITION
            //     data needs TC values too, and no id fix alone will help.
            if (EzThrottler.Throttle($"TcNpc unresolved {npc.NpcId}", 10000))
            {
                var candidates = Svc.Objects
                    .Where(o => o != null && o.ObjectKind == ObjectKind.EventNpc)
                    .Select(o => (o, d: Vector3.Distance(o.Position, npc.Location_Npc)))
                    .OrderBy(t => t.d)
                    .Take(5)
                    .Select(t => $"{t.o.DataId}:{t.o.Name}({t.d:F1}y off)");

                IceLogging.Warning(
                    $"'{npc.Name}' unresolved: id {npc.NpcId} matched nothing and no EventNpc is " +
                    $"within {PositionMatchRadius}y of its recorded position {npc.Location_Npc}. " +
                    $"Nearest EventNpcs by distance FROM THAT POSITION: {string.Join(" | ", candidates)}",
                    "[TcNpc]");
            }
            return false;
        }

        // Throttled per NPC, not globally: resolving several in a row should report each one.
        if (EzThrottler.Throttle($"TcNpc id drift {npc.NpcId}", 30000))
        {
            IceLogging.Warning(
                $"'{npc.Name}' did not match recorded id {npc.NpcId}; resolved by position to " +
                $"BaseId {best.DataId} ('{best.Name}') at {bestDistance:F2}y. " +
                $"Update NpcData.MoonNpcs with {best.DataId} to skip this fallback.", "[TcNpc]");
        }

        gameObject = best;
        return true;
    }
}
