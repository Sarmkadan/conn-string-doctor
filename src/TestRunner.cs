namespace ConnStringDoctor;

public static class TestRunner
{
    public static int Main(string[] args)
    {
        Console.WriteLine("=== ProviderDetector Test Suite ===\n");
        ProviderDetectorTests.RunAll();

        Console.WriteLine("\n=== ConnectionStringRedactor Test Suite ===\n");
        ConnectionStringRedactorTests.RunTests();

        return 0;
    }
}
