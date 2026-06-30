// AI.Infrastructure/Services/LocationScorer.cs
using System.Globalization;

namespace AI.Infrastructure.Services;

internal static class LocationScorer
{
    /// <summary>
    /// Returns 0–100 similarity between two strings.
    /// Returns 50 (neutral) when one side is null/empty — the field was not found in the
    /// contract, which is ambiguous rather than a confirmed mismatch.
    /// Returns 100 when both are null/empty — neither side specified the field.
    /// </summary>
    public static decimal Similarity(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return 100m;
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 50m;

        a = Normalize(a);
        b = Normalize(b);

        if (a == b) return 100m;
        if (a.Contains(b) || b.Contains(a)) return 90m;

        var distance = Levenshtein(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        var ratio = 1.0 - (double)distance / maxLen;
        return (decimal)Math.Round(ratio * 100, 2);
    }

    private static string Normalize(string s)
    {
        var lower = s.Trim().ToLower(CultureInfo.InvariantCulture);

        // Normalise common Arabic variants
        lower = lower
            .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
            .Replace("ى", "ي").Replace("ؤ", "و").Replace("ئ", "ي")
            .Replace("ة", "ه");

        // Remove non-letter punctuation
        lower = new string(lower.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

        // Collapse whitespace
        return string.Join(' ', lower.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }


    private static int Levenshtein(string s, string t)
    {
        var d = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;

        for (var i = 1; i <= s.Length; i++)
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }

        return d[s.Length, t.Length];
    }
    // LocationMatchDetails helper

   
}