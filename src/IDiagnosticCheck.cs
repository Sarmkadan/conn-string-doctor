#nullable enable

namespace ConnStringDoctor;

internal interface IDiagnosticCheck
{
    /// <summary>
    /// Gets the name of the diagnostic check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Runs the diagnostic check asynchronously.
    /// </summary>
    /// <param name="info">Parsed connection string information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to a <see cref="DiagnosticResult"/>.</returns>
    Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct);
}
