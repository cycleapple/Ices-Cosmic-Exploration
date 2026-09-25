// =====================================================================================
// porting-note(api13): TC-only file. NOT from upstream ICE.
//
// A newer FFXIVClientStructs wrapped WKSManager's flat fields into a `State` sub-struct
// (WKSState). FFXIVClientStructs 6966 -- the generation in TC_ok/_dalamud_api13/ -- still
// declares them flat on WKSManager, so upstream's `manager->State.X` is rewritten to
// `manager->X` at the call sites. Those that map 1:1 need nothing from this file:
//
//     State.DevGrade                    -> WKSManager.DevGrade                @0x52
//     State.FishingBait                 -> WKSManager.FishingBait             @0xC4C
//     State.Scores                      -> WKSManager.Scores                  (Span property)
//     State.CurrentMission.MissionUnitRowId -> WKSManager.CurrentMissionUnitRowId @0xC10
//
// ⚠️ CLASS C. FFXIVClientStructs 6966 does NOT map the rest of the "current mission" record
// (score / rank / collected counts). Do not transplant the 7.5 absolute offsets: they are
// measurably different on this game version (CurrentMissionUnitRowId is 0xE30 on 7.5 but
// 0xC10 on TC 7.20; DevGrade 0x0A vs 0x52).
//
// The record is described by TcMissionState below, whose intra-record deltas come from the CS
// source's pre-7.4 revision -- see the doc comment there for the derivation and the two
// cross-checks. The BASE, 0xC10, comes from api13's own metadata and is authoritative.
//
// History worth not repeating: this file originally reused upstream's MissionStateCorrect and
// noted that its deltas were "assumed stable between game 7.2 and 7.5 and NOT verified on a
// live TC client". They were not stable -- the 7.4 pass respaced the record and grew it by 8
// bytes -- and shipping on that assumption produced a silent hang (rank read as 0x50000, ICE
// reporting a mission after one node, the game refusing, the character standing still).
// Registered in scripts/verify-game-axis.py and doc/FORWARD_PORT_REFERENCE.md.
// =====================================================================================

using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using System.Runtime.InteropServices;

namespace ICE.Compat;

/// <summary>
/// The current-mission record as it is laid out on the TC 7.20 binary. This is NOT upstream's
/// <see cref="MissionStateCorrect"/>, which is the post-7.4 shape and does not fit here.
///
/// Sourced from FFXIVClientStructs' own history (JP/Dalamud/lib/FFXIVClientStructs), file
/// FFXIV/Client/Game/WKS/WKSManager.cs, at rev 9e5899625 (2026-02-04) -- the last revision before
/// the 7.4 relayout, which is exactly CLAUDE.md's class-C anchor rule. That revision reads:
///
///     0xC18 CurrentMissionUnitRowId ushort      -> +0
///     0xC20 CurrentScore            ushort      -> +8
///     0xC24 CurrentRank             MissionRank -> +12
///     0xC2A CollectedTotal          ushort      -> +18
///     0xC2C CollectedIndividual     byte        -> +20
///     0xC54 FishingBait             uint        -> +60
///
/// f2634f097 (2026-04-30, the 7.4 pass) moved the whole block to 0xE80 AND respaced it to
/// +12/+16/+22/+24 with FishingBait at +68. api15 ships that newer shape, and upstream ICE's
/// MissionStateCorrect matches it field for field -- which is precisely why it is wrong for TC.
///
/// Two independent cross-checks that this is the right table:
///   1. api13's own metadata puts FishingBait at 0xC4C and CurrentMissionUnitRowId at 0xC10,
///      a delta of +60 -- the pre-7.4 value, not the post-7.4 +68. (The base itself differs by
///      8 from the CS source's 0xC18; yanmucorp regenerated against the TC binary, so the base
///      comes from api13 metadata and only the intra-record deltas come from the CS source.)
///   2. Live memory, 2026-08-04: reading a 4-byte value at +16 returned 0x00050000, i.e. a lone
///      5 at +18 -- CollectedTotal after gathering one node, exactly where this table puts it.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct TcMissionState
{
    [FieldOffset(0)]
    public ushort MissionUnitRowId;

    /// <remarks>ushort on this game version; the post-7.4 layout widened it to uint.</remarks>
    [FieldOffset(8)]
    public ushort Score;

    [FieldOffset(12)]
    public MissionRank Rank;

    [FieldOffset(18)]
    public ushort CollectedTotal;

    [FieldOffset(20)]
    public byte CollectedIndividual;

    /// <remarks>
    /// Upstream's equivalent picks between a ushort and a uint view of the same offset because the
    /// post-7.4 field is 32-bit. Here the field is genuinely 16-bit, so there is nothing to pick.
    /// </remarks>
    public uint EffectiveScore => Score;
}

public static unsafe class TcWks
{
    /// <summary>
    /// Offset of the current-mission record inside WKSManager on the TC 7.20 binary.
    /// Sourced from TC_ok/_dalamud_api13/FFXIVClientStructs.dll, where
    /// <c>WKSManager.CurrentMissionUnitRowId</c> carries <c>[FieldOffset(0xC10)]</c> and is
    /// the first member of that record (MissionStateCorrect.MissionUnitRowId is at 0).
    /// </summary>
    public const int CurrentMissionOffset = 0xC10;

    public static TcMissionState* CurrentMission(WKSManager* manager)
        => manager == null ? null : (TcMissionState*)((byte*)manager + CurrentMissionOffset);

    /// <summary>
    /// The record is provably NOT the 7.5 shape: api15's CS puts FishingBait at rowId+68 while
    /// api13's puts it at rowId+60, so eight bytes were removed somewhere between the two, and
    /// api13's CS maps none of the four fields in between (score / rank / collectedTotal /
    /// collectedIndividual). Runtime confirmed the drift on 2026-08-04: reading Rank as a 4-byte
    /// value at +16 returned 0x00050000, i.e. a lone 5 sitting at +18.
    ///
    /// No rung of CLAUDE.md's class-C ladder covers an unmapped intra-struct offset, so the
    /// remaining source is the live memory itself. Dump the window and correlate each candidate
    /// against what the in-game HUD shows across a mission. Delete once the offsets are pinned.
    /// </summary>
    public static void LogRecordSample(WKSManager* manager, string reason)
    {
        if (manager == null) return;
        var p = (byte*)manager + CurrentMissionOffset;

        var hex = new System.Text.StringBuilder(200);
        for (var i = 0; i < 32; i++)
        {
            if (i % 8 == 0) hex.Append(i == 0 ? "" : " | ").Append($"+{i:D2}:");
            hex.Append($" {p[i]:X2}");
        }

        var u16 = new System.Text.StringBuilder(120);
        for (var i = 10; i <= 26; i += 2)
            u16.Append($" [{i}]={*(ushort*)(p + i)}");

        IceLogging.Info($"WKS record ({reason}) rowId={*(ushort*)p}\n  bytes {hex}\n  u16{u16}", "[TcWks]");
    }
}
