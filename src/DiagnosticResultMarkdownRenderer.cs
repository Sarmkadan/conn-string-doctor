#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnStringDoctor;

/// <summary>
/// Provides functionality to render DiagnosticResult objects in Markdown format.
/// </summary>
internal static class DiagnosticResultMarkdownRenderer
{
    /// <summary>
    /// Renders a collection of DiagnosticResult objects to Markdown format.
    /// </summary>
    /// <param name="results">The diagnostic results to render.</param>
    /// <param name="includeSuccess">Whether to include successful checks in the output.</param>
    /// <returns>Markdown formatted string.</returns>
    public static string Render(IEnumerable<DiagnosticResult> results, bool includeSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(results);

        var builder = new StringBuilder();
        builder.AppendLine("# Connection String Diagnostic Report");
        builder.AppendLine();

        var allResults = results.ToList();
        var failedResults = allResults.Where(r => !r.IsSuccess).ToList();
        var successResults = allResults.Where(r => r.IsSuccess).ToList();

        // Summary section
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- **Total checks:** {allResults.Count}");
        builder.AppendLine($"- **Passed:** {successResults.Count}");
        builder.AppendLine($"- **Failed:** {failedResults.Count}");
        builder.AppendLine();

        // Failed checks section
        if (failedResults.Count > 0)
        {
            builder.AppendLine("## Failed Checks");
            builder.AppendLine();

            foreach (var result in failedResults.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                RenderDiagnosticResult(builder, result, isFailure: true);
                builder.AppendLine();
            }
        }

        // Successful checks section (optional)
        if (includeSuccess && successResults.Count > 0)
        {
            builder.AppendLine("## Successful Checks");
            builder.AppendLine();

            foreach (var result in successResults.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                RenderDiagnosticResult(builder, result, isFailure: false);
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders a single DiagnosticResult to the StringBuilder in Markdown format.
    /// </summary>
    /// <param name="builder">The StringBuilder to append to.</param>
    /// <param name="result">The diagnostic result to render.</param>
    /// <param name="isFailure">Whether this is a failed check.</param>
    private static void RenderDiagnosticResult(StringBuilder builder, DiagnosticResult result, bool isFailure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(result);

        // Check header
        builder.AppendLine($"### {result.Name}");
        builder.AppendLine();

        // Status indicator
        builder.AppendLine($"- **Status:** {(isFailure ? "❌ Failed" : "✅ Passed")}");
        builder.AppendLine();

        // Message
        if (!string.IsNullOrEmpty(result.Message))
        {
            builder.AppendLine("**Message:**");
            builder.AppendLine();
            builder.AppendLine($"> {result.Message.Replace("\n", "\n> ")}");
            builder.AppendLine();
        }

        // Details
        if (!string.IsNullOrEmpty(result.Details))
        {
            builder.AppendLine("**Details:**");
            builder.AppendLine();
            builder.AppendLine(result.Details.Replace("\n", "\n"));
            builder.AppendLine();
        }

        // Errors table
        if (result.Errors.Count > 0)
        {
            builder.AppendLine("**Errors:**");
            builder.AppendLine();
            builder.AppendLine("| # | Error Message |")
                  .AppendLine("|---|-------------|");

            for (int i = 0; i < result.Errors.Count; i++)
            {
                var error = result.Errors[i];
                builder.AppendLine($"| {i + 1} | {EscapeMarkdown(error)} |");
            }
            builder.AppendLine();
        }

        // Warnings table
        if (result.Warnings.Count > 0)
        {
            builder.AppendLine("**Warnings:**");
            builder.AppendLine();
            builder.AppendLine("| # | Warning Message |")
                  .AppendLine("|---|---------------|");

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                var warning = result.Warnings[i];
                builder.AppendLine($"| {i + 1} | {EscapeMarkdown(warning)} |");
            }
            builder.AppendLine();
        }
    }

    /// <summary>
    /// Escapes special Markdown characters in text.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>Escaped text safe for Markdown.</returns>
    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Escape pipe characters and backslashes
        return text.Replace("|", "&#124;")
                  .Replace("\\", "&#92;")
                  .Replace("<", "&lt;")
                  .Replace(">", "&gt;")
                  .Replace("&", "&amp;");
    }

    /// <summary>
    /// Renders diagnostic results to a file in Markdown format.
    /// </summary>
    /// <param name="results">The diagnostic results to render.</param>
    /// <param name="filePath">Path to the output file.</param>
    /// <param name="includeSuccess">Whether to include successful checks in the output.</param>
    public static void RenderToFile(IEnumerable<DiagnosticResult> results, string filePath, bool includeSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(filePath);

        var markdown = Render(results, includeSuccess);
        System.IO.File.WriteAllText(filePath, markdown);
    }
}
