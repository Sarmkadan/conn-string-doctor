namespace ConnStringDoctor;

/// <summary>
/// Tests for FluentConnectionStringBuilder fluent interface and connection string building.
/// </summary>
public static class FluentConnectionStringBuilderTests
{
    public static void RunAll()
    {
        Console.WriteLine("Testing FluentConnectionStringBuilder...\n");

        int passed = 0;
        int failed = 0;

        // Test 1: Basic chaining for SQL Server
        var builder1 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithCredentials("user", "pass")
            .WithTimeout(30);

        string result1 = builder1.Build();
        if (result1.Contains("Server=localhost") &&
            result1.Contains("Database=testdb") &&
            result1.Contains("User Id=user") &&
            result1.Contains("Password=pass") &&
            result1.Contains("Connection Timeout=30"))
        {
            Console.WriteLine("✓ Test 1 PASSED: Basic SQL Server chaining");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 1 FAILED: Basic SQL Server chaining");
            Console.WriteLine($"  Result: {result1}");
            failed++;
        }

        // Test 2: PostgreSQL with port and SSL
        var builder2 = FluentConnectionStringBuilder.For("postgresql")
            .WithHost("db.example.com", 5432)
            .WithDatabase("mydb")
            .WithSsl(false);

        string result2 = builder2.Build();
        if (result2.Contains("Host=db.example.com") &&
            result2.Contains("Port=5432") &&
            result2.Contains("Database=mydb") &&
            result2.Contains("SSL Mode=Disable"))
        {
            Console.WriteLine("✓ Test 2 PASSED: PostgreSQL with port and SSL");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 2 FAILED: PostgreSQL with port and SSL");
            Console.WriteLine($"  Result: {result2}");
            failed++;
        }

        // Test 3: MySQL with integrated security
        var builder3 = FluentConnectionStringBuilder.For("mysql")
            .WithHost("localhost", 3306)
            .WithDatabase("testdb")
            .WithIntegratedSecurity()
            .WithPooling(5, 50);

        string result3 = builder3.Build();
        if (result3.Contains("Server=localhost") &&
            result3.Contains("Port=3306") &&
            result3.Contains("Database=testdb") &&
            result3.Contains("IntegratedSecurity=true") &&
            result3.Contains("Pooling=true") &&
            result3.Contains("Minimum Pool Size=5") &&
            result3.Contains("Maximum Pool Size=50"))
        {
            Console.WriteLine("✓ Test 3 PASSED: MySQL with integrated security and pooling");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 3 FAILED: MySQL with integrated security and pooling");
            Console.WriteLine($"  Result: {result3}");
            failed++;
        }

        // Test 4: SQLite with escaping
        var builder4 = FluentConnectionStringBuilder.For("sqlite")
            .WithDatabase("/path/to/db.sqlite");

        string result4 = builder4.Build();
        if (result4.Contains("Data Source=/path/to/db.sqlite"))
        {
            Console.WriteLine("✓ Test 4 PASSED: SQLite basic");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 4 FAILED: SQLite basic");
            Console.WriteLine($"  Result: {result4}");
            failed++;
        }

        // Test 5: Custom options
        var builder5 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithOption("Application Name", "MyApp")
            .WithOption("MultipleActiveResultSets", "true");

        string result5 = builder5.Build();
        if (result5.Contains("Server=localhost") &&
            result5.Contains("Application Name=MyApp") &&
            result5.Contains("MultipleActiveResultSets=true"))
        {
            Console.WriteLine("✓ Test 5 PASSED: Custom options");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 5 FAILED: Custom options");
            Console.WriteLine($"  Result: {result5}");
            failed++;
        }

        // Test 6: Value escaping with special characters
        var builder6 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("server;name")
            .WithDatabase("db=test")
            .WithCredentials("user name", "pass=word");

        string result6 = builder6.Build();
        if (result6.Contains("Server=\"server;name\"") &&
            result6.Contains("Database=\"db=test\"") &&
            result6.Contains("User Id=\"user name\"") &&
            result6.Contains("Password=\"pass=word\""))
        {
            Console.WriteLine("✓ Test 6 PASSED: Value escaping with special characters");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 6 FAILED: Value escaping with special characters");
            Console.WriteLine($"  Result: {result6}");
            failed++;
        }

        // Test 7: Overwrite semantics - credentials should clear integrated security
        var builder7 = FluentConnectionStringBuilder.For("sqlserver")
            .WithIntegratedSecurity()
            .WithCredentials("user", "pass");

        string result7 = builder7.Build();
        if (result7.Contains("User Id=user") &&
            result7.Contains("Password=pass") &&
            !result7.Contains("Integrated Security"))
        {
            Console.WriteLine("✓ Test 7 PASSED: Credentials overwrite integrated security");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 7 FAILED: Credentials overwrite integrated security");
            Console.WriteLine($"  Result: {result7}");
            failed++;
        }

        // Test 8: Overwrite semantics - integrated security should clear credentials
        var builder8 = FluentConnectionStringBuilder.For("sqlserver")
            .WithCredentials("user", "pass")
            .WithIntegratedSecurity();

        string result8 = builder8.Build();
        if (result8.Contains("Integrated Security=True") &&
            !result8.Contains("User Id") &&
            !result8.Contains("Password"))
        {
            Console.WriteLine("✓ Test 8 PASSED: Integrated security overwrite credentials");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 8 FAILED: Integrated security overwrite credentials");
            Console.WriteLine($"  Result: {result8}");
            failed++;
        }

        // Test 9: Option overwrite semantics
        var builder9 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithOption("Key1", "Value1")
            .WithOption("Key1", "Value2")
            .WithOption("Key2", "Value3");

        string result9 = builder9.Build();
        int key1Count = result9.Split(new[] {"Key1="}, StringSplitOptions.None).Length - 1;
        if (result9.Contains("Key1=Value2") &&
            result9.Contains("Key2=Value3") &&
            key1Count == 1)
        {
            Console.WriteLine("✓ Test 9 PASSED: Option overwrite semantics");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 9 FAILED: Option overwrite semantics");
            Console.WriteLine($"  Result: {result9}");
            failed++;
        }

        // Test 10: Provider-specific ordering - SQL Server
        var builder10 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithCredentials("user", "pass")
            .WithTimeout(60)
            .WithPooling(10, 100);

        string result10 = builder10.Build();
        int serverIndex = result10.IndexOf("Server=");
        int databaseIndex = result10.IndexOf("Database=");
        int userIndex = result10.IndexOf("User Id=");
        int timeoutIndex = result10.IndexOf("Connection Timeout=");
        int poolingIndex = result10.IndexOf("Pooling=");

        if (serverIndex >= 0 &&
            databaseIndex > serverIndex &&
            userIndex > databaseIndex &&
            timeoutIndex > userIndex &&
            poolingIndex > timeoutIndex)
        {
            Console.WriteLine("✓ Test 10 PASSED: SQL Server ordering (Server < Database < User < Timeout < Pooling)");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 10 FAILED: SQL Server ordering");
            Console.WriteLine($"  Result: {result10}");
            failed++;
        }

        // Test 11: Provider-specific ordering - PostgreSQL
        var builder11 = FluentConnectionStringBuilder.For("postgresql")
            .WithHost("localhost")
            .WithDatabase("testdb")
            .WithCredentials("user", "pass")
            .WithTimeout(30);

        string result11 = builder11.Build();
        int hostIndex = result11.IndexOf("Host=");
        int databaseIndex11 = result11.IndexOf("Database=");
        int usernameIndex = result11.IndexOf("Username=");
        int passwordIndex = result11.IndexOf("Password=");
        int timeoutIndex11 = result11.IndexOf("Timeout=");
        int sslIndex = result11.IndexOf("SSL Mode=");

        if (hostIndex >= 0 &&
            databaseIndex11 > hostIndex &&
            usernameIndex > databaseIndex11 &&
            passwordIndex > usernameIndex &&
            timeoutIndex11 > passwordIndex &&
            sslIndex > timeoutIndex11)
        {
            Console.WriteLine("✓ Test 11 PASSED: PostgreSQL ordering (Host < Database < Username < Password < Timeout < SSL Mode)");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 11 FAILED: PostgreSQL ordering");
            Console.WriteLine($"  Result: {result11}");
            failed++;
        }

        // Test 12: Leading/trailing whitespace in values
        var builder12 = FluentConnectionStringBuilder.For("sqlserver")
            .WithHost("  localhost  ")
            .WithDatabase("  testdb  ")
            .WithCredentials("  user  ", "  pass  ");

        string result12 = builder12.Build();
        if (result12.Contains("Server=localhost") &&
            result12.Contains("Database=testdb") &&
            result12.Contains("User Id=user") &&
            result12.Contains("Password=pass"))
        {
            Console.WriteLine("✓ Test 12 PASSED: Leading/trailing whitespace trimmed");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 12 FAILED: Leading/trailing whitespace handling");
            Console.WriteLine($"  Result: {result12}");
            failed++;
        }

        // Test 13: Empty option value should throw
        try
        {
            var builder13 = FluentConnectionStringBuilder.For("sqlserver")
                .WithHost("localhost")
                .WithOption("key", "");
            Console.WriteLine("✗ Test 13 FAILED: Empty option value should throw exception");
            failed++;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ Test 13 PASSED: Empty option value throws exception");
            passed++;
        }

        // Test 14: Null host should throw
        try
        {
            var builder14 = FluentConnectionStringBuilder.For("sqlserver")
                .WithHost(null!);
            Console.WriteLine("✗ Test 14 FAILED: Null host should throw exception");
            failed++;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ Test 14 PASSED: Null host throws exception");
            passed++;
        }

        // Test 15: Invalid pooling configuration should throw
        try
        {
            var builder15 = FluentConnectionStringBuilder.For("sqlserver")
                .WithHost("localhost")
                .WithPooling(100, 50);
            Console.WriteLine("✗ Test 15 FAILED: Invalid pooling (min > max) should throw exception");
            failed++;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ Test 15 PASSED: Invalid pooling throws exception");
            passed++;
        }

        // Test 16: Negative timeout should throw
        try
        {
            var builder16 = FluentConnectionStringBuilder.For("sqlserver")
                .WithHost("localhost")
                .WithTimeout(-1);
            Console.WriteLine("✗ Test 16 FAILED: Negative timeout should throw exception");
            failed++;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ Test 16 PASSED: Negative timeout throws exception");
            passed++;
        }

        // Test 17: Generic provider
        var builder17 = FluentConnectionStringBuilder.For("customdb")
            .WithHost("localhost", 9999)
            .WithDatabase("mydb")
            .WithCredentials("user", "pass");

        string result17 = builder17.Build();
        if (result17.Contains("host=localhost") &&
            result17.Contains("port=9999") &&
            result17.Contains("database=mydb") &&
            result17.Contains("user=user") &&
            result17.Contains("password=pass"))
        {
            Console.WriteLine("✓ Test 17 PASSED: Generic provider uses lowercase keys");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 17 FAILED: Generic provider");
            Console.WriteLine($"  Result: {result17}");
            failed++;
        }

        // Test 18: Validate method detects missing host
        var builder18 = FluentConnectionStringBuilder.For("sqlserver")
            .WithDatabase("testdb");

        var problems18 = builder18.Validate();
        if (problems18.Count == 2 &&
            problems18.Any(p => p.Contains("missing host")) &&
            problems18.Any(p => p.Contains("missing database")))
        {
            Console.WriteLine("✓ Test 18 PASSED: Validate detects missing host");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 18 FAILED: Validate method");
            Console.WriteLine($"  Problems: {string.Join(", ", problems18)}");
            failed++;
        }

        // Test 19: Validate method detects conflicting options
        var builder19 = FluentConnectionStringBuilder.For("sqlserver")
            .WithIntegratedSecurity()
            .WithCredentials("user", "pass");

        var problems19 = builder19.Validate();
        if (problems19.Count == 1 &&
            problems19[0].Contains("conflicting options"))
        {
            Console.WriteLine("✓ Test 19 PASSED: Validate detects conflicting options");
            passed++;
        }
        else
        {
            Console.WriteLine("✗ Test 19 FAILED: Validate conflicting options detection");
            Console.WriteLine($"  Problems: {string.Join(", ", problems19)}");
            failed++;
        }

        // Test 20: Empty provider should throw
        try
        {
            var builder20 = FluentConnectionStringBuilder.For("");
            Console.WriteLine("✗ Test 20 FAILED: Empty provider should throw exception");
            failed++;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("✓ Test 20 PASSED: Empty provider throws exception");
            passed++;
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
