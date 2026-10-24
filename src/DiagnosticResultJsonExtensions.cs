#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnStringDoctor;

/// <summary>
/// JSON serialization for collections of <see cref="DiagnosticResult"/>. Owns the single
/// <see cref="JsonSerializerOptions"/> instance used for diagnostic JSON output, so naming and
/// null-handling rules cannot drift between call sites.
/// </summary>
internal static class DiagnosticResultJsonExtensions
{
    /// <summary>
    /// Shared serializer options: camelCase property names, indented output, and null-valued
    /// properties omitted.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes diagnostic results to an indented JSON array.
    /// </summary>
    /// <param name="results">The diagnostic results to serialize.</param>
    /// <param name="includeSuccess">
    /// When <see langword="true"/>, results that passed are included; otherwise only results with
    /// errors or warnings are written.
    /// </param>
    /// <returns>A JSON array with one object per included result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    internal static string ToJson(this IEnumerable<DiagnosticResult> results, bool includeSuccess)
    {
        ArgumentNullException.ThrowIfNull(results);

        var payload = results
            .Where(r => includeSuccess || !r.IsSuccess)
            .Select(r => new
            {
                name = r.Name,
                severity = r.ResultSeverity.ToString().ToLowerInvariant(),
                message = r.Message,
                isSuccess = r.IsSuccess,
                errors = r.Errors.Count > 0 ? r.Errors.ToArray() : null,
                warnings = r.Warnings.Count > 0 ? r.Warnings.ToArray() : null
            })
            .ToArray();

        return JsonSerializer.Serialize(payload, _options);
    }
}
