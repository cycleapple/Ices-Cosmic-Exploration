// =====================================================================================
// porting-note(api13): TC-only file. NOT from upstream sourpuh/ffxiv_pictomancy.
//
// The TC tree vendors the pre-`PctService`-rename Pictomancy (see doc/SUBMODULE_REGISTRY.md
// "wb-loose"), because every later revision moved to Dalamud.NET.Sdk 14/15 + net10.
// ICE 1.0.0.1 is written against the newer Pictomancy and uses three pieces of surface the
// vendored revision predates:
//
//   * PctDxParams      -- per-draw occlusion hints
//   * AddSphere        -- 3D sphere primitive
//   * a `p:` parameter on the fan/circle primitives
//
// They are added here as a thin compatibility layer rather than by editing ICE, so an
// upstream ICE refresh cannot revert the fix. The occlusion hints are ACCEPTED AND IGNORED:
// the vendored renderer has no occlusion pass, so overlays draw without depth fade. That is
// a cosmetic degradation only.
//
// Delete this file if the vendored Pictomancy is ever advanced past the revision that
// introduced these APIs upstream (`e65d9f5`, 2026-04-27).
// =====================================================================================

using System.Numerics;

namespace Pictomancy;

/// <summary>
/// Occlusion hints accepted for source compatibility with newer Pictomancy. Ignored by the
/// vendored renderer.
/// </summary>
public struct PctDxParams
{
    public float OccludedAlpha { get; set; }
    public float OcclusionTolerance { get; set; }
}

public static class TcPictomancyCompat
{
    /// <summary>
    /// Newer Pictomancy exposes a sphere primitive. The vendored renderer has no sphere
    /// pass, so this approximates it with the filled circle it does have -- visually a flat
    /// disc at the same origin and radius, which is what ICE uses it for (a small node
    /// marker).
    /// </summary>
    public static void AddSphere(this PctDrawList drawList, Vector3 origin, float radius, uint color,
        PctDxParams? p = null)
        => drawList.AddCircleFilled(origin, radius, color);

    /// <summary>Overload accepting (and ignoring) the newer occlusion hints.</summary>
    public static void AddFanFilled(this PctDrawList drawList, Vector3 origin, float innerRadius,
        float outerRadius, float minAngle, float maxAngle, uint color, uint? outerColor,
        PctDxParams? p)
        => drawList.AddFanFilled(origin, innerRadius, outerRadius, minAngle, maxAngle, color, outerColor);

    /// <summary>Overload accepting (and ignoring) the newer occlusion hints.</summary>
    public static void AddCircleFilled(this PctDrawList drawList, Vector3 origin, float radius,
        uint color, uint? outerColor, PctDxParams? p)
        => drawList.AddCircleFilled(origin, radius, color, outerColor);

    /// <summary>
    /// Newer Pictomancy gave AddText's `scale` a default. The vendored revision requires it,
    /// so this overload supplies the same 1.0 default rather than editing the vendored file.
    /// </summary>
    public static void AddText(this PctDrawList drawList, Vector3 position, uint color, string text)
        => drawList.AddText(position, color, text, 1f);
}
