using System;
using System.Collections.Generic;

namespace ConnStringDoctor;

/// <summary>
/// Provides validation helpers for <see cref="ConnectionStringConverter.ConnectionStringInfo"/> and <see cref="ConversionResult"/>.
/// </summary>
public static class ConnectionStringConverterValidation
{
    /// <summary>
    /// Validates the <see cref="ConnectionStringConverter.ConnectionStringInfo"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable problems.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConnectionStringConverter.ConnectionStringInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Provider))
        {
            problems.Add("Provider cannot be null, empty, or whitespace.");
        }

        if (value.OriginalParts is null)
        {
            problems.Add("OriginalParts cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ConnectionStringConverter.ConnectionStringInfo"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if the instance is valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsValid(this ConnectionStringConverter.ConnectionStringInfo value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the <see cref="ConnectionStringConverter.ConnectionStringInfo"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to ensure validity for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this ConnectionStringConverter.ConnectionStringInfo value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException($"Invalid ConnectionStringInfo: {string.Join(", ", problems)}", nameof(value));
        }
    }

    /// <summary>
    /// Validates the <see cref="ConversionResult"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable problems.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConversionResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.ConnectionString))
        {
            problems.Add("ConnectionString cannot be null, empty, or whitespace.");
        }

        if (value.UnmappedKeys is null)
        {
            problems.Add("UnmappedKeys cannot be null.");
        }

        if (value.Warnings is null)
        {
            problems.Add("Warnings cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ConversionResult"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if the instance is valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsValid(this ConversionResult value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the <see cref="ConversionResult"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to ensure validity for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this ConversionResult value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException($"Invalid ConversionResult: {string.Join(", ", problems)}", nameof(value));
        }
    }
}
