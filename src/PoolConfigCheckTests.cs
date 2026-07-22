namespace ConnStringDoctor;

/// <summary>
/// Tests for PoolConfigCheck diagnostic check.
/// </summary>
public static class PoolConfigCheckTests
{
    public static void RunAll()
    {
        Console.WriteLine("Testing PoolConfigCheck...\n");

        int passed = 0;
        int failed = 0;

        // Test 1: Pooling disabled with boolean false
        var result1 = RunCheck("Server=localhost;Pooling=false;");
        if (result1.Errors.Count == 0 && result1.Warnings.Count == 3 &&
            result1.Warnings.Any(w => w.Contains("Pooling is disabled")))
        {
            Console.WriteLine("✓ Test 1 PASSED: Pooling disabled with boolean false");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 1 FAILED: Expected 3 warnings (Pooling disabled + Max Pool Size default + Connect Timeout default), got {result1.Errors.Count} errors and {result1.Warnings.Count} warnings");
            PrintDiagnosticResult(result1);
            failed++;
        }

        // Test 2: Pooling disabled with value "0"
        var result2 = RunCheck("Server=localhost;Pooling=0;");
        if (result2.Errors.Count == 0 && result2.Warnings.Count == 3 &&
            result2.Warnings.Any(w => w.Contains("Pooling is disabled")))
        {
            Console.WriteLine("✓ Test 2 PASSED: Pooling disabled with value '0'");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 2 FAILED: Expected 3 warnings (Pooling disabled + Max Pool Size default + Connect Timeout default), got {result2.Errors.Count} errors and {result2.Warnings.Count} warnings");
            PrintDiagnosticResult(result2);
            failed++;
        }

        // Test 3: MaxPoolSize > 500 should warn
        var result3 = RunCheck("Server=localhost;Max Pool Size=600;");
        if (result3.Errors.Count == 0 && result3.Warnings.Count == 2 &&
            result3.Warnings.Any(w => w.Contains("Max Pool Size is 600")) &&
            result3.Warnings.Any(w => w.Contains("Connect Timeout not specified")))
        {
            Console.WriteLine("✓ Test 3 PASSED: MaxPoolSize > 500 triggers warning");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 3 FAILED: Expected 2 warnings (MaxPoolSize too large + Connect Timeout default), got {result3.Errors.Count} errors and {result3.Warnings.Count} warnings");
            PrintDiagnosticResult(result3);
            failed++;
        }

        // Test 4: MaxPoolSize not specified should warn about default
        var result4 = RunCheck("Server=localhost;");
        if (result4.Errors.Count == 0 && result4.Warnings.Count == 2 &&
            result4.Warnings[0].Contains("Max Pool Size not specified") &&
            result4.Warnings[1].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 4 PASSED: MaxPoolSize not specified triggers default warning");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 4 FAILED: Expected 2 warnings (MaxPoolSize default + Connect Timeout default), got {result4.Errors.Count} errors and {result4.Warnings.Count} warnings");
            PrintDiagnosticResult(result4);
            failed++;
        }

        // Test 5: MinPoolSize > MaxPoolSize should error
        var result5 = RunCheck("Server=localhost;Min Pool Size=200;Max Pool Size=100;");
        if (result5.Errors.Count == 1 && result5.Warnings.Count == 1 &&
            result5.Errors[0].Contains("Min Pool Size (200) is greater than Max Pool Size (100)") &&
            result5.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 5 PASSED: MinPoolSize > MaxPoolSize triggers error");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 5 FAILED: Expected 1 error and 1 warning (Connect Timeout), got {result5.Errors.Count} errors and {result5.Warnings.Count} warnings");
            PrintDiagnosticResult(result5);
            failed++;
        }

        // Test 6: MinPoolSize specified (no error since MaxPoolSize not specified)
        var result6 = RunCheck("Server=localhost;Min Pool Size=50;");
        if (result6.Errors.Count == 0 && result6.Warnings.Count == 2 &&
            result6.Warnings[0].Contains("Max Pool Size not specified") &&
            result6.Warnings[1].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 6 PASSED: MinPoolSize specified without MaxPoolSize");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 6 FAILED: Expected 2 warnings, got {result6.Errors.Count} errors and {result6.Warnings.Count} warnings");
            PrintDiagnosticResult(result6);
            failed++;
        }

        // Test 7: Connect Timeout > 30 should warn
        var result7 = RunCheck("Server=localhost;Connect Timeout=60;");
        if (result7.Errors.Count == 0 && result7.Warnings.Count == 2 &&
            result7.Warnings[0].Contains("Max Pool Size not specified") &&
            result7.Warnings[1].Contains("Connect Timeout is 60 seconds"))
        {
            Console.WriteLine("✓ Test 7 PASSED: Connect Timeout > 30 triggers warning");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 7 FAILED: Expected 2 warnings, got {result7.Errors.Count} errors and {result7.Warnings.Count} warnings");
            PrintDiagnosticResult(result7);
            failed++;
        }

        // Test 8: Connect Timeout not specified should warn
        var result8 = RunCheck("Server=localhost;Max Pool Size=100;");
        if (result8.Errors.Count == 0 && result8.Warnings.Count == 1 &&
            result8.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 8 PASSED: Connect Timeout not specified triggers warning");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 8 FAILED: Expected 1 warning, got {result8.Errors.Count} errors and {result8.Warnings.Count} warnings");
            PrintDiagnosticResult(result8);
            failed++;
        }

        // Test 9: Extreme MaxPoolSize (very large value)
        var result9 = RunCheck("Server=localhost;Max Pool Size=10000;");
        if (result9.Errors.Count == 0 && result9.Warnings.Count == 2 &&
            result9.Warnings[0].Contains("Max Pool Size is 10000") &&
            result9.Warnings[1].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 9 PASSED: Extreme MaxPoolSize (10000) triggers warning");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 9 FAILED: Expected 2 warnings, got {result9.Errors.Count} errors and {result9.Warnings.Count} warnings");
            PrintDiagnosticResult(result9);
            failed++;
        }

        // Test 10: Extreme MinPoolSize (very small value)
        var result10 = RunCheck("Server=localhost;Min Pool Size=0;Max Pool Size=100;");
        if (result10.Errors.Count == 0 && result10.Warnings.Count == 1 &&
            result10.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 10 PASSED: MinPoolSize=0 with valid MaxPoolSize");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 10 FAILED: Expected 1 warning, got {result10.Errors.Count} errors and {result10.Warnings.Count} warnings");
            PrintDiagnosticResult(result10);
            failed++;
        }

        // Test 11: Pooling enabled explicitly
        var result11 = RunCheck("Server=localhost;Pooling=true;Max Pool Size=200;");
        if (result11.Errors.Count == 0 && result11.Warnings.Count == 1 &&
            result11.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 11 PASSED: Pooling enabled explicitly (no pooling warnings)");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 11 FAILED: Expected 1 warning (Connect Timeout), got {result11.Errors.Count} errors and {result11.Warnings.Count} warnings");
            PrintDiagnosticResult(result11);
            failed++;
        }

        // Test 12: Using "Maximum Pool Size" synonym
        var result12 = RunCheck("Server=localhost;Maximum Pool Size=700;");
        if (result12.Errors.Count == 0 && result12.Warnings.Count == 2 &&
            result12.Warnings[0].Contains("Max Pool Size is 700") &&
            result12.Warnings[1].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 12 PASSED: 'Maximum Pool Size' synonym works");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 12 FAILED: Expected 2 warnings, got {result12.Errors.Count} errors and {result12.Warnings.Count} warnings");
            PrintDiagnosticResult(result12);
            failed++;
        }

        // Test 13: Using "Minimum Pool Size" synonym
        var result13 = RunCheck("Server=localhost;Minimum Pool Size=50;Max Pool Size=100;");
        if (result13.Errors.Count == 0 && result13.Warnings.Count == 1 &&
            result13.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 13 PASSED: 'Minimum Pool Size' synonym works");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 13 FAILED: Expected 1 warning, got {result13.Errors.Count} errors and {result13.Warnings.Count} warnings");
            PrintDiagnosticResult(result13);
            failed++;
        }

        // Test 14: MinPoolSize = MaxPoolSize (should not error)
        var result14 = RunCheck("Server=localhost;Min Pool Size=100;Max Pool Size=100;");
        if (result14.Errors.Count == 0 && result14.Warnings.Count == 1 &&
            result14.Warnings[0].Contains("Connect Timeout not specified"))
        {
            Console.WriteLine("✓ Test 14 PASSED: MinPoolSize = MaxPoolSize is valid");
            passed++;
        }
        else
        {
            Console.WriteLine($"✗ Test 14 FAILED: Expected 1 warning, got {result14.Errors.Count} errors and {result14.Warnings.Count} warnings");
            PrintDiagnosticResult(result14);
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

    private static DiagnosticResult RunCheck(string connectionString)
    {
        var info = ConnectionStringParser.Parse(connectionString);
        var check = new PoolConfigCheck();
        var result = check.RunAsync(info, CancellationToken.None).Result;
        return result;
    }

    private static void PrintDiagnosticResult(DiagnosticResult result)
    {
        Console.WriteLine($"  Errors: {result.Errors.Count}");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"    - {error}");
        }
        Console.WriteLine($"  Warnings: {result.Warnings.Count}");
        foreach (var warning in result.Warnings)
        {
            Console.WriteLine($"    - {warning}");
        }
    }
}
