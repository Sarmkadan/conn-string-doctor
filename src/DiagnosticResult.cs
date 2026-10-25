namespace ConnStringDoctor;

/// <summary>
/// Represents the severity level of a diagnostic result.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Informational message - no action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning - potential issue that should be addressed.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error - critical issue that must be fixed.
    /// </summary>
    Error = 2
}

/// <summary>
/// Represents the result of a diagnostic check.
/// </summary>
public sealed class DiagnosticResult
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _errors = new();
    private string? _message;
    private string? _details;
        private Severity _severity = Severity.Info;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticResult"/> class.
    /// </summary>
    /// <param name="checkName">Name of the diagnostic check.</param>
    public DiagnosticResult(string checkName)
    {
        Name = checkName ?? throw new ArgumentNullException(nameof(checkName));
    }

    /// <summary>
    /// Creates an informational result with the given message.
    /// </summary>
    /// <param name="checkName">Name of the diagnostic check.</param>
    /// <param name="message">The informational message.</param>
    /// <param name="recommendation">Optional recommendation appended to the message.</param>
    /// <returns>A result with <see cref="Severity.Info"/> severity.</returns>
    public static DiagnosticResult Info(string checkName, string message, string? recommendation = null)
    {
        var result = new DiagnosticResult(checkName);
        result.SetMessage(Compose(message, recommendation));
        return result;
    }

    /// <summary>
    /// Creates a result containing a single warning.
    /// </summary>
    /// <param name="checkName">Name of the diagnostic check.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="recommendation">Optional recommendation appended to the warning.</param>
    /// <returns>A result with <see cref="Severity.Warning"/> severity.</returns>
    public static DiagnosticResult Warning(string checkName, string message, string? recommendation = null)
    {
        var result = new DiagnosticResult(checkName);
        result.AddWarning(Compose(message, recommendation));
        return result;
    }

    /// <summary>
    /// Creates a result containing a single error.
    /// </summary>
    /// <param name="checkName">Name of the diagnostic check.</param>
    /// <param name="message">The error message.</param>
    /// <param name="recommendation">Optional recommendation appended to the error.</param>
    /// <returns>A result with <see cref="Severity.Error"/> severity.</returns>
    public static DiagnosticResult Error(string checkName, string message, string? recommendation = null)
    {
        var result = new DiagnosticResult(checkName);
        result.AddError(Compose(message, recommendation));
        return result;
    }

    /// <summary>
    /// Gets the name of the diagnostic check.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the message from the diagnostic check.
    /// </summary>
    public string? Message => _message;

    /// <summary>
    /// Gets the detailed information from the diagnostic check.
    /// </summary>
    public string? Details => _details;

    /// <summary>
    /// Gets the list of warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    /// <summary>
    /// Gets the list of errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Gets the severity level of the diagnostic result.
        /// </summary>
        public Severity ResultSeverity => _severity;

    /// <summary>
    /// Gets whether the diagnostic check passed without errors or warnings.
    /// </summary>
    public bool IsSuccess => _errors.Count == 0 && _warnings.Count == 0;

    /// <summary>
    /// Sets the message for the diagnostic result.
    /// </summary>
    /// <param name="message">The message to set.</param>
    public void SetMessage(string message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// Sets the details for the diagnostic result.
    /// </summary>
    /// <param name="details">The details to set.</param>
    public void SetDetails(string details)
    {
        _details = details ?? throw new ArgumentNullException(nameof(details));
    }

    /// <summary>
    /// Adds an error to the diagnostic result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        _errors.Add(error);
        UpdateSeverity();
    }

    /// <summary>
    /// Adds a warning to the diagnostic result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void AddWarning(string warning)
    {
        if (warning is null)
        {
            throw new ArgumentNullException(nameof(warning));
        }

        _warnings.Add(warning);
        UpdateSeverity();
    }

    /// <summary>
    /// Sets the severity level explicitly.
    /// </summary>
    /// <param name="severity">The severity level to set.</param>
    public void SetSeverity(Severity severity)
    {
        _severity = severity;
    }

    /// <summary>
    /// Joins a message and an optional recommendation into a single line of text.
    /// </summary>
    private static string Compose(string message, string? recommendation)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return string.IsNullOrWhiteSpace(recommendation) ? message : $"{message} {recommendation}";
    }

    /// <summary>
    /// Updates the severity based on current errors and warnings.
    /// </summary>
    private void UpdateSeverity()
    {
        if (_errors.Count > 0)
        {
            _severity = Severity.Error;
        }
        else if (_warnings.Count > 0)
        {
            _severity = Severity.Warning;
        }
        else
        {
            _severity = Severity.Info;
        }
    }
}
