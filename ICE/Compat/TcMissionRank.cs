namespace ICE.Compat;

/// <summary>
/// porting-note(api13): TC-only shim. Not from upstream ICE.
///
/// Upstream ICE aliases <c>FFXIVClientStructs.FFXIV.Client.Game.WKS.WKSMissionModule.MissionRank</c>.
/// FFXIVClientStructs 6966 -- the generation shipped in <c>TC_ok/_dalamud_api13/</c> -- predates that
/// enum entirely: neither <c>WKSMissionModule</c> nor <c>WKSManager</c> declares it (metadata-verified
/// against the shipped FFXIVClientStructs.dll).
///
/// The member values below are NOT guessed. They are copied verbatim from the FFXIVClientStructs
/// source clone at <c>JP/Dalamud/lib/FFXIVClientStructs</c> (rev <c>a12e3143d</c>, 2026-05-07),
/// file <c>FFXIV/Client/Game/WKS/WKSManager.cs</c>, where the enum is declared on
/// <c>WKSManager</c>. A later CS generation moved it onto <c>WKSMissionModule</c>, which is the
/// path upstream ICE now aliases; the member set and values are unchanged by that move.
///
/// This is a pure data enum -- it is only compared ordinally and stored in ICE's own
/// hand-authored <see cref="ICE.Utilities.Cosmic_Helper.WKSMissionState"/> explicit layout, so no
/// game struct offset is being invented here.
///
/// Delete this file (and repoint the five call sites back) once the TC reference DLL set ships a
/// FFXIVClientStructs generation that declares MissionRank.
/// </summary>
public enum MissionRank
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Failed = 5,
}
