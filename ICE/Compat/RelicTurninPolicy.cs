using System;
namespace ICE.Compat;

public static class RelicTurninPolicy
{
    public static int NextStage(int stage, int[] needed)
    {
        if (stage < 0 || stage == int.MaxValue || needed == null) return stage;
        foreach (var requirement in needed)
            if (requirement > 0) return stage + 1;
        return stage;
    }

    public static bool ShouldTurnin(int stage, int nextStage, int[] current, int[] needed, bool automatic, int goalTarget)
    {
        if (stage < 0 || nextStage <= stage || (!automatic && goalTarget <= stage)
            || current == null || needed == null || current.Length == 0 || current.Length != needed.Length)
            return false;
        bool hasRequirement = false;
        for (int i = 0; i < needed.Length; i++)
        {
            if (needed[i] < 0 || current[i] < 0) return false;
            if (needed[i] == 0) continue;
            hasRequirement = true;
            if (current[i] < needed[i]) return false;
        }
        return hasRequirement;
    }
}
