#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnStringDoctor;

/// <summary>
/// Provides functionality to render DiagnosticResult objects in HTML format.
/// </summary>
internal static class DiagnosticResultHtmlRenderer
{
    /// <summary>
    /// Renders a collection of DiagnosticResult objects to HTML format.
    /// </summary>
    /// <param name="results">The diagnostic results to render.</param>
    /// <param name="includeSuccess">Whether to include successful checks in the output.</param>
    /// <returns>HTML formatted string.</returns>
    public static string Render(IEnumerable<DiagnosticResult> results, bool includeSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(results);

        var builder = new StringBuilder();

        // HTML header with inline CSS
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("    <meta charset=\"UTF-8\">");
        builder.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        builder.AppendLine("    <title>Connection String Diagnostic Report</title>");
        builder.AppendLine("    <style>");
        builder.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; line-height: 1.6; color: #333; max-width: 1200px; margin: 0 auto; padding: 20px; }");
        builder.AppendLine("        h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; margin-top: 0; }");
        builder.AppendLine("        h2 { color: #2c3e50; margin-top: 30px; border-bottom: 1px solid #eee; padding-bottom: 5px; }");
        builder.AppendLine("        h3 { color: #2c3e50; margin-top: 20px; }");
        builder.AppendLine("        .summary { background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 30px; border-left: 4px solid #3498db; }");
        builder.AppendLine("        .summary-item { margin: 5px 0; }");
        builder.AppendLine("        .summary-item strong { color: #2c3e50; }");
        builder.AppendLine("        .check-card { border: 1px solid #ddd; border-radius: 5px; padding: 20px; margin-bottom: 20px; background-color: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        builder.AppendLine("        .check-header { display: flex; align-items: center; margin-bottom: 15px; }");
        builder.AppendLine("        .check-status { font-weight: bold; margin-left: 10px; padding: 5px 15px; border-radius: 20px; font-size: 0.9em; }");
        builder.AppendLine("        .status-pass { background-color: #d4edda; color: #155724; }");
        builder.AppendLine("        .status-fail { background-color: #f8d7da; color: #721c24; }");
        builder.AppendLine("        .status-error { background-color: #f5c6cb; color: #721c24; font-weight: bold; }");
        builder.AppendLine("        .status-warning { background-color: #fff3cd; color: #856404; font-weight: bold; }");
        builder.AppendLine("        .status-info { background-color: #d1ecf1; color: #0c5460; }");
        builder.AppendLine("        .message { background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #2196F3; }");
        builder.AppendLine("        .details { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #6c757d; }");
        builder.AppendLine("        .errors, .warnings { margin-top: 15px; }");
        builder.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 10px 0; }");
        builder.AppendLine("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }");
        builder.AppendLine("        th { background-color: #f8f9fa; font-weight: 600; color: #2c3e50; }");
        builder.AppendLine("        tr:hover { background-color: #f5f5f5; }");
        builder.AppendLine("        .severity-error { color: #dc3545; font-weight: bold; }");
        builder.AppendLine("        .severity-warning { color: #ffc107; font-weight: bold; }");
        builder.AppendLine("        .severity-info { color: #17a2b8; }");
        builder.AppendLine("        .footer { margin-top: 50px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 0.9em; }");
        builder.AppendLine("    </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("    <h1>Connection String Diagnostic Report</h1>");
        builder.AppendLine();

        var allResults = results.ToList();
        if (allResults.Any(result => result is null))
        {
            throw new ArgumentException("The results collection cannot contain null elements.", nameof(results));
        }

        var failedResults = allResults.Where(r => !r.IsSuccess).ToList();
        var successResults = allResults.Where(r => r.IsSuccess).ToList();

        // Summary section
        builder.AppendLine("    <div class=\"summary\">");
        builder.AppendLine("        <h2>Summary</h2>");
        builder.AppendLine("        <div class=\"summary-item\"><strong>Total checks:</strong> " + allResults.Count + "</div>");
        builder.AppendLine("        <div class=\"summary-item\"><strong>Passed:</strong> " + successResults.Count + "</div>");
        builder.AppendLine("        <div class=\"summary-item\"><strong>Failed:</strong> " + failedResults.Count + "</div>");
        builder.AppendLine("    </div>");
        builder.AppendLine();

        // Failed checks section
        if (failedResults.Count > 0)
        {
            builder.AppendLine("    <h2>Failed Checks</h2>");
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
            builder.AppendLine("    <h2>Successful Checks</h2>");
            builder.AppendLine();

            foreach (var result in successResults.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            {
                RenderDiagnosticResult(builder, result, isFailure: false);
                builder.AppendLine();
            }
        }

        builder.AppendLine("    <div class=\"footer\">");
        builder.AppendLine("        <p>Generated by Connection String Doctor at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</p>");
        builder.AppendLine("    </div>");

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    /// <summary>
    /// Renders a single DiagnosticResult to the StringBuilder in HTML format.
    /// </summary>
    /// <param name="builder">The StringBuilder to append to.</param>
    /// <param name="result">The diagnostic result to render.</param>
    /// <param name="isFailure">Whether this is a failed check.</param>
    private static void RenderDiagnosticResult(StringBuilder builder, DiagnosticResult result, bool isFailure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(result);

        // Determine severity class
        string severityClass = result.ResultSeverity switch
        {
            Severity.Error => "severity-error",
            Severity.Warning => "severity-warning",
            _ => "severity-info"
        };

        // Check header with status
        builder.AppendLine("    <div class=\"check-card\">");
        builder.AppendLine("        <div class=\"check-header\">");
        builder.AppendLine("            <h3>" + EscapeHtml(result.Name) + " <span class=\"severity-info\">(" + result.ResultSeverity + ")</span></h3>");
        builder.AppendLine("            <span class=\"check-status status-" + (isFailure ? "fail" : "pass") + "\">" + (isFailure ? "❌ Failed" : "✅ Passed") + "</span>");
        builder.AppendLine("        </div>");

        // Message
        if (!string.IsNullOrEmpty(result.Message))
        {
            builder.AppendLine("        <div class=\"message\">");
            builder.AppendLine("            <strong>Message:</strong><br/>");
            builder.AppendLine("            " + FormatHtmlText(result.Message) + "");
            builder.AppendLine("        </div>");
        }

        // Details
        if (!string.IsNullOrEmpty(result.Details))
        {
            builder.AppendLine("        <div class=\"details\">");
            builder.AppendLine("            <strong>Details:</strong><br/>");
            builder.AppendLine("            " + FormatHtmlText(result.Details) + "");
            builder.AppendLine("        </div>");
        }

        // Errors table
        if (result.Errors.Count > 0)
        {
            builder.AppendLine("        <div class=\"errors\">");
            builder.AppendLine("            <strong>Errors:</strong>");
            builder.AppendLine("            <table>");
            builder.AppendLine("                <thead><tr><th>#</th><th>Error Message</th></tr></thead>");
            builder.AppendLine("                <tbody>");

            for (int i = 0; i < result.Errors.Count; i++)
            {
                var error = result.Errors[i];
                builder.AppendLine("                <tr><td>" + (i + 1) + "</td><td>" + EscapeHtml(error) + "</td></tr>");
            }

            builder.AppendLine("                </tbody>");
            builder.AppendLine("            </table>");
            builder.AppendLine("        </div>");
        }

        // Warnings table
        if (result.Warnings.Count > 0)
        {
            builder.AppendLine("        <div class=\"warnings\">");
            builder.AppendLine("            <strong>Warnings:</strong>");
            builder.AppendLine("            <table>");
            builder.AppendLine("                <thead><tr><th>#</th><th>Warning Message</th></tr></thead>");
            builder.AppendLine("                <tbody>");

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                var warning = result.Warnings[i];
                builder.AppendLine("                <tr><td>" + (i + 1) + "</td><td>" + EscapeHtml(warning) + "</td></tr>");
            }

            builder.AppendLine("                </tbody>");
            builder.AppendLine("            </table>");
            builder.AppendLine("        </div>");
        }

        builder.AppendLine("    </div>");
    }

    /// <summary>
    /// Escapes HTML special characters in text.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>Escaped text safe for HTML.</returns>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return System.Net.WebUtility.HtmlEncode(text);
    }

    /// <summary>
    /// Formats text for HTML output, converting newlines to <br/> tags.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>Formatted HTML text.</returns>
    private static string FormatHtmlText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return EscapeHtml(text).Replace("\n", "<br/>");
    }

    /// <summary>
    /// Renders diagnostic results to a file in HTML format.
    /// </summary>
    /// <param name="results">The diagnostic results to render.</param>
    /// <param name="filePath">Path to the output file.</param>
    /// <param name="includeSuccess">Whether to include successful checks in the output.</param>
    public static void RenderToFile(IEnumerable<DiagnosticResult> results, string filePath, bool includeSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(filePath);

        var html = Render(results, includeSuccess);
        System.IO.File.WriteAllText(filePath, html);
    }
}
