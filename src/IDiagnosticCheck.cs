namespace ConnStringDoctor;

/// <summary>
/// Interface for diagnostic checks that analyze connection strings.
/// </summary>
internal interface IDiagnosticCheck
{
    /// <summary>
    /// Gets the name of the diagnostic check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Runs the diagnostic check asynchronously.
    /// </summary>
    /// <param name="connectionString">The connection string to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Diagnostic result with analysis.</returns>
    ValueTask<DiagnosticResult> RunAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}
