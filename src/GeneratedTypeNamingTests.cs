using System.Reflection;

namespace ConnStringDoctor;

/// <summary>
/// Verifies that the mechanically generated companion types (JsonExtensions, Extensions,
/// Tests, Validation) in this assembly are never applied to their own output - i.e. no
/// public type name contains one of these suffixes repeated back to back.
/// </summary>
public static class GeneratedTypeNamingTests
{
    /// <summary>
    /// Suffixes that mark a type as already generated. Any generation convention that
    /// produces <c>*JsonExtensions</c>, <c>*Extensions</c>, <c>*Tests</c>, or
    /// <c>*Validation</c> companion types must call <see cref="IsAlreadyGenerated"/> on
    /// the source type name first, and skip generation when it returns <see langword="true"/>,
    /// so that a generated type is never fed back into the generator as if it were
    /// original source.
    /// </summary>
    public static readonly IReadOnlyList<string> GeneratedSuffixes =
    [
        "JsonExtensions",
        "Extensions",
        "Tests",
        "Validation",
    ];

    /// <summary>
    /// Determines whether a source type name already ends in one of the recognized
    /// generated-type suffixes (<see cref="GeneratedSuffixes"/>), meaning it is itself
    /// the output of a previous generation pass and must not be generated from again.
    /// </summary>
    /// <param name="typeName">The candidate source type name to check.</param>
    /// <returns><see langword="true"/> if generation should be skipped for this type; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="typeName"/> is <see langword="null"/> or empty.</exception>
    public static bool IsAlreadyGenerated(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        return GeneratedSuffixes.Any(suffix => typeName.EndsWith(suffix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Determines whether a type name ends with the given suffix doubled back to back,
    /// e.g. <c>"FooJsonExtensionsJsonExtensions"</c> for suffix <c>"JsonExtensions"</c>.
    /// </summary>
    /// <param name="typeName">The type name to inspect.</param>
    /// <param name="suffix">The generated-type suffix to check for duplication.</param>
    /// <returns><see langword="true"/> if the suffix appears twice consecutively at the end of the name.</returns>
    /// <exception cref="ArgumentException"><paramref name="typeName"/> or <paramref name="suffix"/> is <see langword="null"/> or empty.</exception>
    public static bool HasRepeatedSuffix(string typeName, string suffix)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentException.ThrowIfNullOrEmpty(suffix);

        var doubled = suffix + suffix;
        return typeName.EndsWith(doubled, StringComparison.Ordinal);
    }

    /// <summary>
    /// Scans every public type in the <see cref="ConnStringDoctor"/> assembly and fails
    /// loudly (via console output and a non-zero count) if any type name contains a
    /// doubled generated-suffix, such as <c>FooJsonExtensionsJsonExtensions</c>.
    /// </summary>
    /// <returns><see langword="true"/> if every public type name is clean; otherwise, <see langword="false"/>.</returns>
    public static bool RunAll()
    {
        int passed = 0;
        int failed = 0;

        var publicTypes = typeof(GeneratedTypeNamingTests).Assembly.GetTypes()
            .Where(t => t.IsPublic);

        foreach (var type in publicTypes)
        {
            var offendingSuffix = GeneratedSuffixes.FirstOrDefault(suffix => HasRepeatedSuffix(type.Name, suffix));

            if (offendingSuffix is null)
            {
                passed++;
                continue;
            }

            failed++;
            Console.WriteLine($"✗ FAILED: '{type.Name}' contains a repeated '{offendingSuffix}' suffix");
        }

        Console.WriteLine(failed == 0
            ? $"✓ PASSED: {passed} public type(s) checked, no repeated generated suffixes found"
            : $"{passed} passed, {failed} failed");

        return failed == 0;
    }
}
