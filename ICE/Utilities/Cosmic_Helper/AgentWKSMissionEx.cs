using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.STD;
using System.Runtime.InteropServices;

namespace ICE.Utilities.Cosmic_Helper;

/// <summary>
/// Shim for AgentWKSMission members not yet in the shipped FFXIVClientStructs DLL.
/// Remove once upstream CS ships GetCriticalMissions.
/// </summary>
public static unsafe class AgentWKSMissionEx
{
    private delegate bool GetCriticalMissionsDelegate(AgentWKSMission* agent, StdVector<AgentWKSMission.MissionEntry>* list);
    private static readonly nint _wksAetheryteBase;

    private delegate bool GetMasterMissionsDelegate(AgentWKSMission* agent, StdVector<AgentWKSMission.MissionEntry>* list);

    private static readonly GetCriticalMissionsDelegate? _getCriticalMissions;
    private static readonly GetCriticalMissionsDelegate? _getMasteryMissions;

    static AgentWKSMissionEx()
    {
        try
        {
            var ptr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC ?? 48 8B EA E8");
            _getCriticalMissions = Marshal.GetDelegateForFunctionPointer<GetCriticalMissionsDelegate>(ptr);
        }
        catch (Exception ex)
        {
            IceLogging.Error($"{ex.Message} | [AgentWKSMissionEx] Failed to scan GetCriticalMissions sig");
        }

        // porting-note(TC game 7.20): upstream sig-scans for JobIndexToClassJobId with a pattern of
        // SIX bytes (movzx eax,dl / cmp eax,0Ah) and no wildcards -- a fragment common
        // enough that ScanText's first hit lands on an unrelated function on the TC binary. The
        // call then returns garbage, the classJobId loop in SetSelectedJobTab never matches, and
        // jobIndex silently falls back to 0 -- so the agent always selects the FIRST job tab.
        // Crafters happened to still work; gathering missions never appeared and nothing moved.
        //
        // No scan is needed at all: FFXIVClientStructs 6966 (the build shipped in
        // TC_ok/_dalamud_api13, generated for THIS game version) already declares
        // AgentWKSMission.JobIndexToClassJobId as a [MemberFunction] with the authoritative sig
        // "E8 ?? ?? ?? ?? 3A D8 74 ?? 41 FE C6". That is CLAUDE.md's class-C rung 0, and it is
        // strictly better than any hand-written sig because it moves with the reference DLL set.
        // Do NOT reintroduce the scan on refresh -- guarded in scripts/verify-game-axis.py.

        try
        {
            var ptr = Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC ?? 4C 8B F2 48 8B D9 E8 ?? ?? ?? ?? 48 8B 4B");
            _getMasteryMissions = Marshal.GetDelegateForFunctionPointer<GetCriticalMissionsDelegate>(ptr);
        }
        catch(Exception ex)
        {
            // porting-note(TC game 7.20): this sig does not resolve and is not knowable -- upstream
            // never rescanned it after the game version this pattern was captured on, so no rung of
            // CLAUDE.md's class-C ladder has an answer (CS 6966 has no [MemberFunction] for it, and
            // the JP history's last touch predates 7.20). GetMasterMissions() already degrades to
            // `return false` via the null check, so the feature is dead but nothing throws.
            // Logged as a one-shot Warning rather than an Error: it fired on every load and read as
            // a live fault while the actual stalls were elsewhere.
            IceLogging.Warning($"{ex.Message} | [AgentWKSMissionEx] MasterMission sig unresolved on this game version -- master-mission listing is disabled");
        }

        try
        {
            _wksAetheryteBase = Svc.SigScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? 48 8B F8");
        }
        catch (Exception ex)
        {
            IceLogging.Error($"{ex.Message} | [AgentWKSMissionEx] Failed to scan WKSAetheryte sig");
        }
    }

    /// <summary>
    /// Populates <paramref name="list"/> with the current critical missions, same as
    /// GetBasicMissions / GetProvisionalMissions. Returns false if the sig wasn't found.
    /// </summary>
    public static bool GetCriticalMissions(AgentWKSMission* agent, StdVector<AgentWKSMission.MissionEntry>* list)
    {
        if (_getCriticalMissions == null || agent == null) return false;
        return _getCriticalMissions(agent, list);
    }

    public static bool GetMasterMissions(AgentWKSMission* agent, StdVector<AgentWKSMission.MissionEntry>* list)
    {
        if (_getMasteryMissions == null || agent == null) return false;
        return _getMasteryMissions(agent, list);
    }

    /// <summary>
    /// Sets the agent's selected job tab by ClassJob ID (8–18), resolving it to the
    /// internal 0–11 job index. Also syncs MissionData and clears HasSavedTab.
    /// Returns false if the sig wasn't found or the agent/data is null.
    /// </summary>
    public static bool SetSelectedJobTab(AgentWKSMission* agent, byte classJobId, byte categoryTab = 0)
    {
        if (agent == null || agent->Data == null) return false;

        byte jobIndex = 0;
        for (byte i = 0; i <= 11; i++)
        {
            // CS member function, not a hand-written sig -- see the note in the static ctor.
            if (agent->JobIndexToClassJobId(i) != classJobId) continue;
            jobIndex = i;
            break;
        }

        agent->SelectedTab = categoryTab;
        agent->Data->SelectedJobIndex = jobIndex;
        agent->Data->UpdateFlags = 1;
        // Clear HasSavedTab to prevent the game from overwriting our set on the next tick
        // HasSavedTab is private so we write it directly via offset (0x35)
        *(bool*)((byte*)agent + 0x35) = false;

        return true;
    }

    public static int selectedTab()
    {
        var agent = AgentWKSMission.Instance();

        if (agent == null || agent->Data == null) return -1;
        return agent->SelectedTab;
    }

    public static bool IsWKSAetheryteUnlocked(byte rowId)
    {
        if (_wksAetheryteBase == nint.Zero) return false;

        var addr = *(nint*)_wksAetheryteBase + 3788;
        if (addr == nint.Zero) return false;

        var value = *(uint*)addr;
        return (value & (1u << (rowId - 1))) != 0;
    }
}