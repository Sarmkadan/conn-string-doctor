using Xunit;

namespace ConnStringDoctor.Tests;

/// <summary>
/// Guards the shipped library/CLI assembly against accidental leakage of test helper
/// types into its public surface.
/// </summary>
public class AssemblySurfaceTests
{
    [Fact]
    public void MainAssembly_ExportsNoTypeEndingInTests()
    {
        var mainAssembly = typeof(ProviderDetector).Assembly;

        var offendingTypes = mainAssembly.GetExportedTypes()
            .Where(t => t.Name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(offendingTypes.Length == 0,
            $"main assembly exports test type(s): {string.Join(", ", offendingTypes)}");
    }

    /// <summary>
    /// Suffixes that a mechanical code generator may append to a type name (e.g. to produce
    /// a "*JsonExtensions" companion class). None of these should ever appear twice in a row
    /// within the same type name - that indicates the generator re-processed its own output.
    /// </summary>
    private static readonly string[] _generatedSuffixes = ["JsonExtensions", "Tests", "Extensions", "Validation"];

    [Fact]
    public void MainAssembly_ExportsNoTypeWithRepeatedGeneratorSuffix()
    {
        var mainAssembly = typeof(ProviderDetector).Assembly;

        var offendingTypes = mainAssembly.GetExportedTypes()
            .Where(t => _generatedSuffixes.Any(suffix =>
                t.Name.EndsWith(suffix + suffix, StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(offendingTypes.Length == 0,
            $"main assembly exports type(s) with a doubled generator suffix: {string.Join(", ", offendingTypes)}");
    }
}
