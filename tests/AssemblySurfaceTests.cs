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
}
