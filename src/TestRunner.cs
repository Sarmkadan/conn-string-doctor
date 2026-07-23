namespace ConnStringDoctor;

public static class TestRunner
{
    public static int Main(string[] args)
    {
        Console.WriteLine("=== ProviderDetector Test Suite ===\n");
        ProviderDetectorTests.RunAll();

        Console.WriteLine("\n=== ConnectionStringRedactor Test Suite ===\n");
        ConnectionStringRedactorTests.RunTests();

        Console.WriteLine("\n=== PoolConfigCheck Test Suite ===\n");
        PoolConfigCheckTests.RunAll();

        Console.WriteLine("\n=== Generated Type Naming Test Suite ===\n");
        var namingClean = GeneratedTypeNamingTests.RunAll();

        return namingClean ? 0 : 1;
    }
}
