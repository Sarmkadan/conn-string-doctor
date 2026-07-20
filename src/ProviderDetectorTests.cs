namespace ConnStringDoctor;

/// <summary>
/// Simple tests to verify ProviderDetector functionality.
/// </summary>
public static class ProviderDetectorTests
{
    public static void RunAll()
    {
        Console.WriteLine("Testing ProviderDetector...\n");

        int passed = 0;
        int failed = 0;

        // Test 1: SQL Server with Integrated Security
        var result1 = ProviderDetector.DetectProvider("Server=localhost;Database=test;Integrated Security=True");
        if (result1.Provider == DbProvider.SqlServer && result1.Confidence > 90)
        {
            Console.WriteLine("✓ Test 1 PASSED: SQL Server with Integrated Security");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 1 FAILED: Expected SqlServer with high confidence, got {result1.Provider} with {result1.Confidence}");
            failed++;
        }

        // Test 2: SQL Server with Trusted_Connection
        var result2 = ProviderDetector.DetectProvider("Data Source=myServerAddress;Initial Catalog=myDataBase;Trusted_Connection=True;");
        if (result2.Provider == DbProvider.SqlServer && result2.Confidence > 90)
        {
            Console.WriteLine("✓ Test 2 PASSED: SQL Server with Trusted_Connection");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 2 FAILED: Expected SqlServer with high confidence, got {result2.Provider} with {result2.Confidence}");
            failed++;
        }

        // Test 3: PostgreSQL with SSL Mode
        var result3 = ProviderDetector.DetectProvider("Host=localhost;Port=5432;Database=test;Username=user;Ssl Mode=Require;");
        if (result3.Provider == DbProvider.PostgreSql && result3.Confidence > 90)
        {
            Console.WriteLine("✓ Test 3 PASSED: PostgreSQL with SSL Mode");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 3 FAILED: Expected PostgreSql with high confidence, got {result3.Provider} with {result3.Confidence}");
            failed++;
        }

        // Test 4: PostgreSQL with default port
        var result4 = ProviderDetector.DetectProvider("Server=localhost;Port=5432;Database=test;");
        if (result4.Provider == DbProvider.PostgreSql && result4.Confidence > 90)
        {
            Console.WriteLine("✓ Test 4 PASSED: PostgreSQL with default port");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 4 FAILED: Expected PostgreSql with high confidence, got {result4.Provider} with {result4.Confidence}");
            failed++;
        }

        // Test 5: MySQL with default port
        var result5 = ProviderDetector.DetectProvider("Server=localhost;Port=3306;Database=test;");
        if (result5.Provider == DbProvider.MySql && result5.Confidence > 90)
        {
            Console.WriteLine("✓ Test 5 PASSED: MySQL with default port");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 5 FAILED: Expected MySql with high confidence, got {result5.Provider} with {result5.Confidence}");
            failed++;
        }

        // Test 6: MySQL with user keyword
        var result6 = ProviderDetector.DetectProvider("Host=localhost;User=root;Database=test;");
        if (result6.Provider == DbProvider.MySql && result6.Confidence > 80)
        {
            Console.WriteLine("✓ Test 6 PASSED: MySQL with User keyword");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 6 FAILED: Expected MySql with high confidence, got {result6.Provider} with {result6.Confidence}");
            failed++;
        }

        // Test 7: SQLite with file extension
        var result7 = ProviderDetector.DetectProvider("Data Source=/path/to/database.db;");
        if (result7.Provider == DbProvider.Sqlite && result7.Confidence > 90)
        {
            Console.WriteLine("✓ Test 7 PASSED: SQLite with .db extension");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 7 FAILED: Expected Sqlite with high confidence, got {result7.Provider} with {result7.Confidence}");
            failed++;
        }

        // Test 8: SQLite with .sqlite extension
        var result8 = ProviderDetector.DetectProvider("Data Source=/path/to/database.sqlite;");
        if (result8.Provider == DbProvider.Sqlite && result8.Confidence > 90)
        {
            Console.WriteLine("✓ Test 8 PASSED: SQLite with .sqlite extension");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 8 FAILED: Expected Sqlite with high confidence, got {result8.Provider} with {result8.Confidence}");
            failed++;
        }

        // Test 9: Generic connection string (should default to SQL Server)
        var result9 = ProviderDetector.DetectProvider("Server=localhost;Database=test;");
        if (result9.Provider == DbProvider.SqlServer && result9.Confidence > 70)
        {
            Console.WriteLine("✓ Test 9 PASSED: Generic defaults to SQL Server");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 9 FAILED: Expected SqlServer with medium confidence, got {result9.Provider} with {result9.Confidence}");
            failed++;
        }

        // Test 10: Unknown/empty
        var result10 = ProviderDetector.DetectProvider("");
        if (result10.Provider == DbProvider.Unknown && result10.Confidence == 0)
        {
            Console.WriteLine("✓ Test 10 PASSED: Empty string returns Unknown");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 10 FAILED: Expected Unknown with 0 confidence, got {result10.Provider} with {result10.Confidence}");
            failed++;
        }

        // Test 11: DefaultPort method
        int sqlServerPort = ProviderDetector.DefaultPort(DbProvider.SqlServer);
        if (sqlServerPort == 1433)
        {
            Console.WriteLine("✓ Test 11 PASSED: DefaultPort for SqlServer is 1433");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 11 FAILED: Expected 1433, got {sqlServerPort}");
            failed++;
        }

        int postgresPort = ProviderDetector.DefaultPort(DbProvider.PostgreSql);
        if (postgresPort == 5432)
        {
            Console.WriteLine("✓ Test 12 PASSED: DefaultPort for PostgreSql is 5432");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 12 FAILED: Expected 5432, got {postgresPort}");
            failed++;
        }

        Console.WriteLine($"\n=== Test Results ===");
        Console.WriteLine($"Passed: {passed}");
        Console.WriteLine($"Failed: {failed}");
        Console.WriteLine($"Total: {passed + failed}");

        if (failed > 0)
        {
            Environment.Exit(1);
        }
    }
}
