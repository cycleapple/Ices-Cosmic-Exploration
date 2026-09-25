// =====================================================================================
// porting-note(api13): TC-only file. NOT from upstream ICE.
//
// Upstream ICE is written against the Lumina.Excel generation that ships with Dalamud
// api15 / game 7.5, where the WKS* sheets have real column names. The Lumina.Excel in
// TC_ok/_dalamud_api13/ was generated from the PRE-7.4 EXDSchema, where most of those
// columns are still called UnknownN.
//
// Rather than editing the ~30 upstream call sites (which an upstream refresh would
// silently revert), this file re-exposes the api15 names as C# 14 extension members.
// Upstream files compile unchanged; a refresh cannot drop the fix, because nothing in
// an upstream file was touched.
//
// HOW THE MAPPING WAS DERIVED (no guessing -- see doc/FORWARD_PORT_REFERENCE.md):
//   1. The api13 property lists were read out of TC_ok/_dalamud_api13/Lumina.Excel.dll
//      by reflection, and match the PRE-7.4 EXDSchema yml files name-for-name.
//   2. The 7.5 names came from Lumina.Excel/deps/EXDSchema (submodule of JP/Dalamud,
//      rev a12e3143d) and from the api15 Lumina.Excel.dll's property order.
//   3. Both lists were aligned position-by-position; every mapping below either lands on
//      a surviving same-name anchor (e.g. WKSMissionReward.Unknown19 <-> Unknown19) or was
//      confirmed against real TC row values from HoshinoLYK/ffxiv-datamining-tc
//      (csv/, last commit "Update Patch v7.20").
//   4. Columns the TC binary genuinely does not have return a documented default; they
//      are marked ABSENT-ON-TC below.
//
// Re-verify every entry here on a TC API/game bump. See scripts/verify-game-axis.py.
// =====================================================================================

global using ICE.Compat;

using Dalamud.Utility;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace ICE.Compat;

/// <summary>
/// Minimal stand-in for Lumina's <c>RowRef&lt;T&gt;</c>. Upstream reaches these columns as
/// row references (<c>.RowId</c>, sometimes <c>.Value</c>); at api13 the same columns are
/// plain integers, so the reference is reconstructed here.
/// </summary>
public readonly struct TcRowRef<T>(uint rowId) where T : struct, IExcelRow<T>
{
    public uint RowId { get; } = rowId;

    /// <summary>
    /// porting-note(api13): deliberately <c>GetRowOrDefault</c>, not <c>GetRow</c>. TC is game
    /// 7.2 content, so rows that exist on 7.5 (anything belonging to Phaenna / Oizys /
    /// Auxesia) are simply absent here. Degrading to a zeroed row keeps the inert-planet UI
    /// rendering blanks instead of throwing mid-frame -- see CLAUDE.md §4 step 7 B1.
    /// </summary>
    public T Value
    {
        get
        {
            var sheet = Svc.Data.GetExcelSheet<T>();
            // Falling back to `default(T)` looks safe but is a landmine: a default Lumina row has a
            // null ExcelPage, so the FIRST column read NREs deep inside Lumina rather than yielding a
            // zero. Row 0 exists in every WKS sheet and is all-zeroes, so it degrades the way callers
            // actually expect. (This is the same shape as the upstream Shop_Tokens bug that NRE'd in
            // Item.get_ItemAction().)
            return sheet.GetRowOrDefault(RowId) ?? sheet.GetRowOrDefault(0) ?? default;
        }
    }

    public T? ValueNullable => Svc.Data.GetExcelSheet<T>().GetRowOrDefault(RowId);
}

/// <summary>Fixed 3-element view over three separate api13 columns.</summary>
public readonly struct TcTriple<T>(T a, T b, T c)
{
    private readonly T _a = a, _b = b, _c = c;

    public int Count => 3;

    public T this[int i] => i switch
    {
        0 => _a,
        1 => _b,
        2 => _c,
        _ => throw new IndexOutOfRangeException($"TcTriple index {i} out of range"),
    };
}

public static class TcApi13SheetCompat
{
    // -- WKSMissionReward -------------------------------------------------------------
    // api13 (pre-7.4 schema, 17 columns) vs 7.5 (18 columns). Anchor: Unknown19 keeps its
    // name in BOTH generations and lands at the same position, which fixes the alignment
    // of the whole 7-byte tail. The single extra 7.5 column is BaseDronebits, added by
    // EXDSchema commit 502cab3 "Update WKSMissionReward.yml to include BaseDronebits"
    // (2026-02-07, "Replaced 'Unknown20'") -- dronebits are an Oizys/Auxesia mechanic that
    // the TC 7.20 client does not have, and the TC sheet indeed has 17 columns.
    extension(WKSMissionReward r)
    {
        public TcRowRef<Item> Item => new(r.Unknown15);
        public TcTriple<ushort> ExpModifier => new(r.Unknown16, r.Unknown17, r.Unknown18);
        public ushort CosmoCredits => r.Unknown0;
        public ushort PlanetCredits => r.Unknown1;
        public TcTriple<ushort> ResearchReward => new(r.Unknown2, r.Unknown3, r.Unknown4);
        public ushort ItemCount => r.Unknown8;
        public TcTriple<TcRowRef<WKSCosmoToolClass>> Tool =>
            new(new(r.Unknown9), new(r.Unknown10), new(r.Unknown11));
        public TcTriple<byte> TypeIndex => new(r.Unknown12, r.Unknown13, r.Unknown14);

        /// <summary>ABSENT-ON-TC: added in game 7.4. Always 0 here.</summary>
        public ushort BaseDronebits => 0;
    }

    // -- WKSMissionMapMarker ----------------------------------------------------------
    // 4 columns in both generations; confirmed against TC rows, e.g. row 1 =
    // (0, 905, 849, 100) -> Icon 0, X 905, Y 849, Radius 100.
    extension(WKSMissionMapMarker m)
    {
        public uint Icon => m.Unknown0;
        public ushort X => m.Unknown1;
        public ushort Y => m.Unknown2;
        public ushort Radius => m.Unknown3;
    }

    // -- WKSMissionLotterySpecialCond -------------------------------------------------
    // 3 columns in both. Confirmed against TC rows: the two byte columns hold 2-hour
    // windows ((0,2), (2,4), (4,6) ...), the uint16 column is 0 throughout (no weather
    // requirement), which matches WeatherRequired / StartTimeHour / EndTimeHour.
    extension(WKSMissionLotterySpecialCond c)
    {
        public TcRowRef<Weather> WeatherRequired => new(c.Unknown0);
        public byte StartTimeHour => c.Unknown1;
        public byte EndTimeHour => c.Unknown2;
    }

    // -- WKSMissionToDo ---------------------------------------------------------------
    // Aligned through position 10; positions 4-9 (RequiredItem, RequiredItemQuantity,
    // Unknown9..Unknown12) keep identical names in both generations and pin the alignment.
    // 7.5 inserted two uint16 columns AFTER MapMarker, so only the head is comparable --
    // which is all upstream needs here.
    extension(WKSMissionToDo t)
    {
        public TcRowRef<Lumina.Excel.Sheets.Action> TemporaryAction => new(t.Unknown0);
        public TcRowRef<WKSMissionMapMarker> MapMarker => new(t.Unknown13);
    }

    // -- WKSScoreList -----------------------------------------------------------------
    // api13 has 5 columns, 7.5 has 7 (Unknown5 and Unknown6 were added). Unknown0..Unknown4
    // keep their names, so only the two new ones are missing.
    extension(WKSScoreList s)
    {
        /// <summary>ABSENT-ON-TC: added after game 7.3. Always 0 here.</summary>
        public uint Unknown5 => 0;
    }

    // -- WKSTerritoryInfo -------------------------------------------------------------
    // 13 columns in both; every other field name is identical, so position 8
    // (api13 Unknown0 <-> 7.5 TerritoryType) is unambiguous. Confirmed against TC row 2,
    // whose value is 1237 == Sinus Ardorum's TerritoryType row id.
    extension(WKSTerritoryInfo t)
    {
        public TcRowRef<Lumina.Excel.Sheets.TerritoryType> TerritoryType => new(t.Unknown0);
    }

    // -- ItemAction -------------------------------------------------------------------
    // 7.5 renamed the leading uint16 discriminator `Type` to `Action` and typed it as a
    // row reference into the Action sheet. Same column, same position.
    extension(ItemAction a)
    {
        public TcRowRef<Lumina.Excel.Sheets.Action> Action => new(a.Type);
    }
}
