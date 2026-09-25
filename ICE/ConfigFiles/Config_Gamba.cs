using System.Collections.Generic;

namespace ICE.ConfigFiles;

public partial class Config
{
    public List<Gamba> GambaItemWeights { get; set; } = new();
    public bool GambaEnabled { get; set; } = false;
    public bool GambaPreferSmallerWheel { get; set; } = false;
    public int GambaCreditsMinimum { get; set; } = 0;
    public int GambaDelay { get; set; } = 250;
    public bool GambaBetweenRuns { get; set; } = false;
    public int GambaAtAmount { get; set; } = 1000;

    public class Gamba
    {
        public uint ItemId { get; set; }
        public int Weight { get; set; } = 0;
        public GambaType Type { get; set; }
    }

    public enum GambaType
    {
        Mount = 0,
        Emote = 1,
        Minion = 2,
        Outfit = 3,
        Accessory = 4,
        Orchestrion = 5,
        Housing = 6,
        Dye = 7,
        Other = 8,
        Materia = 9,
    }
}
