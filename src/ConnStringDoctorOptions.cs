#nullable enable

namespace ConnStringDoctor;

/// <summary>
/// Options for configuring ConnStringDoctor services.
/// </summary>
public sealed class ConnStringDoctorOptions
{
    /// <summary>
    /// The mask string used to replace sensitive values in connection strings.
    /// Defaults to "***".
    /// </summary>
    public string Mask { get; set; } = "***";

    /// <summary>
    /// The list of keys considered sensitive (case-insensitive) for redaction.
    /// If empty, validation will fail.
    /// </summary>
    public IList<string> SecretKeys { get; set; } = new List<string> { "Password", "Pwd" };

    /// <summary>
    /// The database provider name (e.g., "SqlServer", "MySql"). If specified, must be a known provider.
    /// </summary>
    public string? Provider { get; set; }
}