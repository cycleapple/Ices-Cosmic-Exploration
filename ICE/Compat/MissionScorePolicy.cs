namespace ICE.Compat;

public static class MissionScorePolicy
{
    public static MissionRank Classify(uint? score, uint bronze, uint? silver, uint? gold, bool canReport)
    {
        if (score is not { } current) return MissionRank.None;
        if (gold is > 0 && current >= gold.Value) return MissionRank.Gold;
        if (silver is > 0 && current >= silver.Value) return MissionRank.Silver;
        if (bronze > 0 && current >= bronze || canReport) return MissionRank.Bronze;
        return MissionRank.None;
    }
}
