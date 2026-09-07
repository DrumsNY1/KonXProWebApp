using KonXProWebApp.Models.db_9f8bee_konxdev;

#nullable enable

namespace KonXProWebApp.Services;

/// <summary>
/// Utility helpers for transforming raw DOB job descriptions into
/// trade-first summaries suitable for scanning by contractors.
/// </summary>
public static class DescriptionHelper
{
    private const int DefaultMaxChars = 80;

    /// <summary>
    /// Truncates raw description text at a word boundary with an ellipsis.
    /// Returns null for empty/null input.
    /// </summary>
    public static string? Truncate(string? text, int maxChars = DefaultMaxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        if (trimmed.Length <= maxChars)
            return trimmed;

        // Find the last space before the limit to avoid cutting mid-word
        var cutoff = trimmed.LastIndexOf(' ', maxChars);
        if (cutoff <= 0)
            cutoff = maxChars; // No space found — hard cut

        return string.Concat(trimmed.AsSpan(0, cutoff), "…");
    }

    /// <summary>
    /// Builds a trade-first one-line summary from structured filing fields.
    /// Format: "Plumbing, Sprinkler · $64,000 · Major Alteration"
    /// Falls back to truncated JobDescription when no trades are flagged.
    /// </summary>
    public static string Summarize(DobjobFiling filing)
    {
        ArgumentNullException.ThrowIfNull(filing);

        var parts = new List<string>(3);

        // 1. Collect active trades from boolean flags
        var trades = GetActiveTrades(filing);
        if (trades.Count > 0)
        {
            parts.Add(string.Join(", ", trades));
        }

        // 2. Estimated cost
        if (filing.InitialCost.HasValue && filing.InitialCost.Value > 0)
        {
            parts.Add(filing.InitialCost.Value.ToString("C0"));
        }

        // 3. Job type mapped to friendly name
        var friendlyType = MapJobType(filing.JobType);
        if (friendlyType is not null)
        {
            parts.Add(friendlyType);
        }

        // If we built a structured summary, return it
        if (parts.Count > 0)
            return string.Join(" · ", parts);

        // Fallback: truncated raw description
        return Truncate(filing.JobDescription, DefaultMaxChars)
               ?? "No description available";
    }

    private static List<string> GetActiveTrades(DobjobFiling filing)
    {
        var trades = new List<string>(9);

        if (filing.Plumbing == "X") trades.Add("Plumbing");
        if (filing.Mechanical == "X") trades.Add("Mechanical");
        if (filing.Boiler == "X") trades.Add("Boiler");
        if (filing.Sprinkler == "X") trades.Add("Sprinkler");
        if (filing.FireAlarm == "X") trades.Add("Fire Alarm");
        if (filing.Standpipe == "X") trades.Add("Standpipe");
        if (filing.Equipment == "X") trades.Add("Equipment");
        if (filing.FireSuppression == "X") trades.Add("Fire Suppression");
        if (filing.CurbCut == "X") trades.Add("Curb Cut");

        return trades;
    }

    private static string? MapJobType(string? jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType))
            return null;

        return jobType.Trim().ToUpperInvariant() switch
        {
            "A1" => "Major Alteration",
            "A2" => "Minor Alteration",
            "A3" => "Minor Alteration",
            "NB" => "New Building",
            "DM" => "Demolition",
            "SG" => "Sign",
            _ => jobType.Trim()
        };
    }
}
