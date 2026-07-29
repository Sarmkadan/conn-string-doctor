using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnStringDoctor
{
    /// <summary>
    /// Extension methods for <see cref="DiagnosticResult"/> and collections of <see cref="DiagnosticResult"/>.
    /// </summary>
    public static class DiagnosticResultExtensions
    {
        /// <summary>
        /// Determines whether the diagnostic result contains any errors.
        /// </summary>
        /// <param name="result">The diagnostic result.</param>
        /// <returns><c>true</c> if there is at least one error; otherwise, <c>false</c>.</returns>
        public static bool HasErrors(this DiagnosticResult result)
        {
            if (result is null) throw new ArgumentNullException(nameof(result));
            return result.Errors.Any();
        }

        /// <summary>
        /// Returns the worst (most severe) severity among a sequence of diagnostic results.
        /// If the sequence is empty, <see cref="Severity.Info"/> is returned.
        /// </summary>
        /// <param name="results">The diagnostic results.</param>
        /// <returns>The highest severity present in the collection.</returns>
        public static Severity WorstSeverity(this IEnumerable<DiagnosticResult> results)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));

            // If there are no results, default to Info.
            if (!results.Any())
                return Severity.Info;

            // Severity enum values are ordered Info=0, Warning=1, Error=2.
            // Max will give the worst severity.
            return results.Max(r => r.ResultSeverity);
        }

        /// <summary>
        /// Generates a one‑line summary for a diagnostic result.
        /// The format is: "<c>Name: Severity - Message</c>".
        /// If <see cref="DiagnosticResult.Message"/> is <c>null</c>, the message part is omitted.
        /// </summary>
        /// <param name="result">The diagnostic result.</param>
        /// <returns>A concise summary string.</returns>
        public static string ToSummaryLine(this DiagnosticResult result)
        {
            if (result is null) throw new ArgumentNullException(nameof(result));

            var baseLine = $"{result.Name}: {result.ResultSeverity}";
            return result.Message is null ? baseLine : $"{baseLine} - {result.Message}";
        }

        /// <summary>
        /// Filters a collection of diagnostic results to only those with the specified severity.
        /// </summary>
        /// <param name="results">The diagnostic results.</param>
        /// <param name="severity">The severity to filter by.</param>
        /// <returns>An <see cref="IEnumerable{DiagnosticResult}"/> containing only results with the given severity.</returns>
        public static IEnumerable<DiagnosticResult> FilterBySeverity(this IEnumerable<DiagnosticResult> results, Severity severity)
        {
            if (results is null) throw new ArgumentNullException(nameof(results));
            return results.Where(r => r.ResultSeverity == severity);
        }
    }
}
