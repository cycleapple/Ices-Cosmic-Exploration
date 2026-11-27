using System.Collections.Generic;
using System.Linq;
namespace ICE.Utilities;
public static class FishingBaitPolicy
{
    public static uint Select(uint current, IEnumerable<uint> available)
    {
        var baits = available.Where(x => x != 0).ToArray();
        return current != 0 && baits.Contains(current) ? current : baits.FirstOrDefault();
    }
}
