using System.Globalization;
using System.Text.RegularExpressions;

namespace ECommons.UIHelpers.AddonMasterImplementations;

public static class MissionScoreText
{
    public static uint? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        // A single number with optional labels/group separators; never concatenate a ratio.
        var match = Regex.Match(text, @"^\D*?([0-9]+(?:[,，][0-9]{3})*)\D*?$", RegexOptions.CultureInvariant);
        if (!match.Success) return null;
        var number = match.Groups[1].Value.Replace(",", "").Replace("，", "");
        return uint.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
