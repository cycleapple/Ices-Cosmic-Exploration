// =====================================================================================
// porting-note(api13): TC-only file. NOT from upstream ICE.
//
// The `WKSAetheryte` Excel sheet DOES NOT EXIST on the TC client. Verified two ways:
//   * TC_ok/_dalamud_api13/Lumina.Excel.dll declares no WKSAetheryte row type;
//   * HoshinoLYK/ffxiv-datamining-tc (csv/, last commit "Update Patch v7.20") dumps 7803
//     sheets and WKSAetheryte.csv is not among them -- while every other WKS* sheet ICE
//     reads IS present.
//
// This is not a porting gap: moon aethernet shards arrived with Phaenna in game 7.3, and
// TC is 7.2 content. Sinus Ardorum -- the only Cosmic Exploration zone the TC client has --
// has no aethernet network at all, so an empty MoonAethernet dictionary is the CORRECT
// state here, and Task_NavmeshMove falls back to walking.
//
// Upstream's UpdateDict() loop body is deliberately left intact and simply iterates an
// empty list, so a future refresh keeps working and the feature switches itself on the day
// TC ships the sheet (at which point these stubs get replaced by the real Lumina types).
// =====================================================================================

namespace ICE.Compat;

public readonly struct TcWksAetheryteObject
{
    public uint Unknown0 { get; }
}

public readonly struct TcWksAetheryteObjectGroupRef
{
    public IReadOnlyList<TcWksAetheryteObject> Value => [];
}

public readonly struct TcWksAetherytePlaceName
{
    public string Name => string.Empty;
}

public readonly struct TcWksAetherytePlaceNameRef
{
    public TcWksAetherytePlaceName Value => default;
}

/// <summary>Shape-compatible stand-in for the absent WKSAetheryte row; never enumerated.</summary>
public readonly struct TcWksAetheryteRow
{
    public uint RowId { get; }
    public TcWksAetherytePlaceNameRef Name { get; }
    public TcWksAetheryteObjectGroupRef ObjectGroup { get; }
}
