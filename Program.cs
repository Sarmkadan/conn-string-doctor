using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConnStringDoctor;

var rootCommand = new RootCommand("Connection String Doctor - Diagnoses connection strings for common issues and compares them");

// Define --list-checks option
var listChecksOption = new Option<bool>(
    name: "--list-checks",
    description: "List all available diagnostic checks with their descriptions");

// Define options for diagnose command
var connectionStringOption = new Option<string>(
    name: "--connection-string",
    description: "The connection string to diagnose");

var formatOption = new Option<string>(
    name: "--format",
    description: "Output format (text, markdown, html, json)",
    getDefaultValue: () => "text");

var outputOption = new Option<FileInfo?>(
    name: "--output",
    description: "Output file path (optional)");

var includeSuccessOption = new Option<bool>(
    name: "--include-success",
    description: "Include successful checks in output",
    getDefaultValue: () => false);

var failOnOption = new Option<Severity>(
    name: "--fail-on",
    description: "Exit with code based on highest diagnostic severity: 0=ok (Info or lower), 1=warning, 2=error. If any diagnostic meets or exceeds this severity level, exit with corresponding code",
    getDefaultValue: () => Severity.Info);

// Define options for compare command
var connectionStringAOption = new Option<string>(
    name: "--connection-string-a",
    description: "First connection string to compare");

var connectionStringBOption = new Option<string>(
    name: "--connection-string-b",
    description: "Second connection string to compare");

var redactOption = new Option<bool>(
    name: "--redact",
    description: "Redact sensitive information (passwords, tokens, etc.)",
    getDefaultValue: () => true);

var showAllOption = new Option<bool>(
    name: "--show-all",
    description: "Show all keys including matching ones",
    getDefaultValue: () => false);

var normalizeRedactOption = new Option<bool>(
    name: "--redact",
    description: "Redact sensitive information (passwords, tokens, etc.)",
    getDefaultValue: () => true);

var normalizeConnectionStringOption = new Option<string>(
    name: "--connection-string",
    description: "The connection string to normalize");

// Create diagnose command
var diagnoseCommand = new Command("diagnose", "Diagnose a connection string for common issues");
diagnoseCommand.AddOption(connectionStringOption);
diagnoseCommand.AddOption(formatOption);
diagnoseCommand.AddOption(outputOption);
diagnoseCommand.AddOption(includeSuccessOption);
diagnoseCommand.AddOption(failOnOption);

diagnoseCommand.SetHandler(async (context) =>
{
    try
    {
        // Parse arguments
        var connectionString = context.ParseResult.GetValueForOption(connectionStringOption) ?? string.Empty;
        var format = context.ParseResult.GetValueForOption(formatOption);
        var outputFile = context.ParseResult.GetValueForOption(outputOption);
        var includeSuccess = context.ParseResult.GetValueForOption(includeSuccessOption);
        var failOnSeverity = context.ParseResult.GetValueForOption(failOnOption);

        // Parse the connection string using ConnectionStringParser
        var parsed = ConnectionStringParser.Parse(connectionString);

        // Get all available diagnostic checks
        var checks = new IDiagnosticCheck[]
        {
            new DeprecatedKeywordCheck(),
            new UnknownKeywordCheck(),
            new DuplicateKeyCheck(),
            new PoolConfigAnalyzer(),
            new TimeoutConfigCheck(),
            new TimeoutSanityCheck(),
            new DnsAndTcpCheck(),
            new TlsInspector()
        };

        var results = new List<DiagnosticResult>();

        // Run all checks
        foreach (var check in checks)
        {
            var result = await check.RunAsync(parsed, CancellationToken.None);
            results.Add(result);
        }

        // Output based on format
        string output;
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            output = FormatAsJson(results, includeSuccess);
        }
        else if (format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
        {
            output = DiagnosticResultMarkdownRenderer.Render(results, includeSuccess);
        }
        else if (format.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            output = DiagnosticResultHtmlRenderer.Render(results, includeSuccess);
        }
        else
        {
            // Default text format
            output = FormatAsText(results, includeSuccess);
        }

        // Write to output file or console
        if (outputFile != null)
        {
            await File.WriteAllTextAsync(outputFile.FullName, output);
            Console.WriteLine($"Report written to: {outputFile.FullName}");
        }
        else
        {
            Console.WriteLine(output);
        }

        // Check if we should fail based on severity
        // Map severity to exit codes: 0=ok (Info or lower), 1=warning, 2=error
        var maxSeverity = results.Max(r => r.ResultSeverity);
        var exitCode = maxSeverity switch
        {
            Severity.Info => 0,
            Severity.Warning => 1,
            Severity.Error => 2,
            _ => 0
        };

        if (exitCode > 0)
        {
            Environment.Exit(exitCode);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

// Create compare command
var compareCommand = new Command("compare", "Compare two connection strings key-by-key");
compareCommand.AddOption(connectionStringAOption);
compareCommand.AddOption(connectionStringBOption);
compareCommand.AddOption(redactOption);
compareCommand.AddOption(showAllOption);
compareCommand.AddOption(outputOption);

compareCommand.SetHandler((context) =>
{
    try
    {
        var connectionStringA = context.ParseResult.GetValueForOption(connectionStringAOption);
        var connectionStringB = context.ParseResult.GetValueForOption(connectionStringBOption);
        var redact = context.ParseResult.GetValueForOption(redactOption);
        var showAll = context.ParseResult.GetValueForOption(showAllOption);
        var outputFile = context.ParseResult.GetValueForOption(outputOption);

        // Compare the connection strings
        var output = ConnectionStringComparator.Compare(connectionStringA, connectionStringB, redact, showAll);

        // Write to output file or console
        if (outputFile != null)
        {
            File.WriteAllText(outputFile.FullName, output);
            Console.WriteLine($"Comparison written to: {outputFile.FullName}");
        }
        else
        {
            Console.WriteLine(output);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

// Create normalize command
var normalizeCommand = new Command("normalize", "Normalize a connection string with canonical key names, sorted keys, and consistent spacing");
normalizeCommand.AddOption(normalizeConnectionStringOption);
normalizeCommand.AddOption(normalizeRedactOption);
normalizeCommand.AddOption(outputOption);

normalizeCommand.SetHandler((context) =>
{
    try
    {
        var connectionString = context.ParseResult.GetValueForOption(normalizeConnectionStringOption) ?? string.Empty;
        var redact = context.ParseResult.GetValueForOption(normalizeRedactOption);
        var outputFile = context.ParseResult.GetValueForOption(outputOption);

        // Normalize the connection string
        var normalized = ConnectionStringNormalizer.Normalize(connectionString, redact);

        // Write to output file or console
        if (outputFile != null)
        {
            File.WriteAllText(outputFile.FullName, normalized);
            Console.WriteLine($"Normalized connection string written to: {outputFile.FullName}");
        }
        else
        {
            Console.WriteLine(normalized);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

// Create list-checks command
var listChecksCommand = new Command("list-checks", "List all available diagnostic checks with their descriptions");
listChecksCommand.AddOption(listChecksOption);

listChecksCommand.SetHandler((context) =>
{
    try
    {
        // Get all available diagnostic checks
        var checks = new IDiagnosticCheck[]
        {
            new DeprecatedKeywordCheck(),
            new UnknownKeywordCheck(),
            new DuplicateKeyCheck(),
            new PoolConfigAnalyzer(),
            new TimeoutConfigCheck(),
            new TimeoutSanityCheck(),
            new DnsAndTcpCheck(),
            new TlsInspector()
        };

        // Sort by name for consistent output
        var sortedChecks = checks.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        // Write output
        Console.WriteLine("Available diagnostic checks:");
        Console.WriteLine();
        foreach (var check in sortedChecks)
        {
            Console.WriteLine($" {check.Name}");
            Console.WriteLine($" {check.Description}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {sortedChecks.Count} checks");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
});

// Create selftest command
var selftestCommand = new Command("selftest", "Run built-in self-tests for core components");
selftestCommand.SetHandler(() =>
{
    var exitCode = RunSelftest();
    Environment.Exit(exitCode);
});

// Add subcommands to root
rootCommand.AddCommand(diagnoseCommand);
rootCommand.AddCommand(compareCommand);
rootCommand.AddCommand(normalizeCommand);
rootCommand.AddCommand(listChecksCommand);
rootCommand.AddCommand(selftestCommand);

return await rootCommand.InvokeAsync(args);

static string FormatAsText(List<DiagnosticResult> results, bool includeSuccess)
{
    var builder = new System.Text.StringBuilder();

    foreach (var result in results.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
    {
        if (!includeSuccess && result.IsSuccess)
        {
            continue;
        }

        builder.AppendLine($"Check: {result.Name}");
        builder.AppendLine($" Status: {(result.IsSuccess ? "PASS" : "FAIL")}");

        if (!string.IsNullOrEmpty(result.Message))
        {
            builder.AppendLine($" Message: {result.Message}");
        }

        if (result.Errors.Count > 0)
        {
            builder.AppendLine($" Errors ({result.Errors.Count}):");
            foreach (var error in result.Errors)
            {
                builder.AppendLine($" - {error}");
            }
        }

        if (result.Warnings.Count > 0)
        {
            builder.AppendLine($" Warnings ({result.Warnings.Count}):");
            foreach (var warning in result.Warnings)
            {
                builder.AppendLine($" - {warning}");
            }
        }

        builder.AppendLine();
    }

    return builder.ToString();
}

static string FormatAsJson(List<DiagnosticResult> results, bool includeSuccess)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = includeSuccess ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never
    };

    var jsonResults = results
        .Where(r => includeSuccess || !r.IsSuccess)
        .Select(r => new
        {
            name = r.Name,
            severity = r.ResultSeverity.ToString().ToLowerInvariant(),
            message = r.Message,
            isSuccess = r.IsSuccess,
            errors = r.Errors.Count > 0 ? r.Errors.ToArray() : null,
            warnings = r.Warnings.Count > 0 ? r.Warnings.ToArray() : null
        })
        .ToArray();

    return JsonSerializer.Serialize(jsonResults, options);
}

static int RunSelftest()
{
    int passed = 0;
    int failed = 0;

    Console.WriteLine("Running self-tests...");
    Console.WriteLine();

    // Test ConnectionStringParser
    Console.WriteLine("Testing ConnectionStringParser...");
    passed += RunParserTests();

    // Test ConnectionStringRedactor
    Console.WriteLine("\nTesting ConnectionStringRedactor...");
    passed += RunRedactorTests();

    // Test ConnectionStringConverter
    Console.WriteLine("\nTesting ConnectionStringConverter...");
    passed += RunConverterTests();

    Console.WriteLine();
    Console.WriteLine($"Results: {passed} passed, {failed} failed");

    if (failed > 0)
    {
        Console.WriteLine("SELFTEST FAILED");
        return 1;
    }
    else
    {
        Console.WriteLine("SELFTEST PASSED");
        return 0;
    }
}

static int RunParserTests(bool expectFailure = false)
{
    int passed = 0;
    int failed = 0;

    void Test(string description, Func<bool> testFunc)
    {
        try
        {
            bool result = testFunc();
            if (result == !expectFailure)
            {
                Console.WriteLine($"  PASS: {description}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            if (expectFailure)
            {
                Console.WriteLine($"  PASS (expected failure): {description} - {ex.Message}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description} - {ex.Message}");
                failed++;
            }
        }
    }

    // Test 1: Quoted values
    Test("Parses quoted values correctly", () =>
    {
        var info = ConnectionStringParser.Parse(@"Server=""my server,port"";Database=test;");
        return info.Server == "my server,port" && info.Database == "test";
    });

    // Test 2: Escaped semicolons
    Test("Handles escaped semicolons in values", () =>
    {
        var info = ConnectionStringParser.Parse(@"Server=server\;port;Database=test");
        return info.Server == @"server\;port" && info.Database == "test";
    });

    // Test 3: Alias mapping - Server/Data Source
    Test("Maps Server and Data Source aliases correctly", () =>
    {
        var info1 = ConnectionStringParser.Parse("Server=localhost;Database=test");
        var info2 = ConnectionStringParser.Parse("Data Source=localhost;Database=test");
        return info1.Server == info2.Server && info1.Database == info2.Database;
    });

    // Test 4: Alias mapping - User ID variations
    Test("Maps User ID, Uid, User, Username aliases correctly", () =>
    {
        var info1 = ConnectionStringParser.Parse("User ID=admin;Password=secret");
        var info2 = ConnectionStringParser.Parse("Uid=admin;Password=secret");
        var info3 = ConnectionStringParser.Parse("User=admin;Password=secret");
        var info4 = ConnectionStringParser.Parse("Username=admin;Password=secret");
        return info1.User == info2.User && info2.User == info3.User && info3.User == info4.User;
    });

    // Test 5: Alias mapping - Password variations
    Test("Maps Password and Pwd aliases correctly", () =>
    {
        var info1 = ConnectionStringParser.Parse("User ID=admin;Password=secret");
        var info2 = ConnectionStringParser.Parse("User ID=admin;Pwd=secret");
        return info1.Password == info2.Password;
    });

    // Test 6: Port parsing
    Test("Parses port correctly", () =>
    {
        var info = ConnectionStringParser.Parse("Server=localhost;Port=1433;Database=test");
        return info.Port == 1433;
    });

    // Test 7: Host and port extraction from comma-separated
    Test("Extracts host and port from comma-separated value", () =>
    {
        var info = ConnectionStringParser.Parse("Server=localhost,1433;Database=test");
        return info.Server == "localhost" && info.Port == 1433;
    });

    // Test 8: Host and port extraction from colon-separated (non-IPv6)
    Test("Extracts host and port from colon-separated value", () =>
    {
        var info = ConnectionStringParser.Parse("Server=localhost:1433;Database=test");
        return info.Server == "localhost" && info.Port == 1433;
    });

    // Test 9: IPv6 address handling
    Test("Handles IPv6 addresses correctly", () =>
    {
        var info = ConnectionStringParser.Parse(@"Server=[2001:db8::1]:1433;Database=test");
        return info.Server == "2001:db8::1" && info.Port == 1433;
    });

    // Test 10: Empty connection string handling
    Test("Throws exception for null/empty connection string", () =>
    {
        try
        {
            ConnectionStringParser.Parse(null);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    });

    return expectFailure ? failed : passed;
}

static int RunRedactorTests(bool expectFailure = false)
{
    int passed = 0;
    int failed = 0;

    void Test(string description, Func<bool> testFunc)
    {
        try
        {
            bool result = testFunc();
            if (result == !expectFailure)
            {
                Console.WriteLine($"  PASS: {description}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            if (expectFailure)
            {
                Console.WriteLine($"  PASS (expected failure): {description} - {ex.Message}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description} - {ex.Message}");
                failed++;
            }
        }
    }

    // Test 1: Full redaction of password
    Test("Fully redacts password values", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Password=secret123";
        var redacted = ConnectionStringRedactor.Redact(original, RedactionMode.Full, "****");
        return !redacted.Contains("secret123") && redacted.Contains("Password=****");
    });

    // Test 2: Partial redaction of password
    Test("Partially redacts password values correctly", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Password=secret123";
        var redacted = ConnectionStringRedactor.Redact(original, RedactionMode.Partial, "****");
        // Should keep first 2 and last 2 chars: se****23
        return redacted.Contains("Password=se****23");
    });

    // Test 3: Redaction of Pwd alias
    Test("Redacts Pwd alias correctly", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Pwd=secret123";
        var redacted = ConnectionStringRedactor.Redact(original);
        return !redacted.Contains("secret123") && redacted.Contains("Pwd=****");
    });

    // Test 4: Redaction of User ID
    Test("Redacts User ID correctly", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Password=secret";
        var redacted = ConnectionStringRedactor.Redact(original);
        return !redacted.Contains("admin") && redacted.Contains("User ID=****");
    });

    // Test 5: Redaction of User alias
    Test("Redacts User alias correctly", () =>
    {
        var original = "Server=localhost;Database=test;User=admin;Password=secret";
        var redacted = ConnectionStringRedactor.Redact(original);
        return !redacted.Contains("admin") && redacted.Contains("User=****");
    });

    // Test 6: Redaction of Token
    Test("Redacts Token values", () =>
    {
        var original = "Server=localhost;Database=test;Token=mytoken123";
        var redacted = ConnectionStringRedactor.Redact(original);
        return !redacted.Contains("mytoken123") && redacted.Contains("Token=****");
    });

    // Test 7: Redaction of Secret
    Test("Redacts Secret values", () =>
    {
        var original = "Server=localhost;Database=test;Secret=mysecret123";
        var redacted = ConnectionStringRedactor.Redact(original);
        return !redacted.Contains("mysecret123") && redacted.Contains("Secret=****");
    });

    // Test 8: No redaction when no secrets present
    Test("Does not modify connection string when no secrets present", () =>
    {
        var original = "Server=localhost;Database=test;Port=1433";
        var redacted = ConnectionStringRedactor.Redact(original);
        return original == redacted;
    });

    // Test 9: RedactToDictionary functionality
    Test("RedactToDictionary returns correct dictionary", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Password=secret123";
        var dict = ConnectionStringRedactor.RedactToDictionary(original);
        return dict["User ID"] == "****" && dict["Password"] == "****" &&
               dict["Server"] == "localhost" && dict["Database"] == "test";
    });

    // Test 10: RedactKeepUser functionality
    Test("RedactKeepUser redacts only password", () =>
    {
        var original = "Server=localhost;Database=test;User ID=admin;Password=secret123";
        var redacted = ConnectionStringRedactor.RedactKeepUser(original);
        return redacted.Contains("User ID=admin") && !redacted.Contains("secret123") &&
               redacted.Contains("Password=***");
    });

    return expectFailure ? failed : passed;
}

static int RunConverterTests(bool expectFailure = false)
{
    int passed = 0;
    int failed = 0;

    void Test(string description, Func<bool> testFunc)
    {
        try
        {
            bool result = testFunc();
            if (result == !expectFailure)
            {
                Console.WriteLine($"  PASS: {description}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            if (expectFailure)
            {
                Console.WriteLine($"  PASS (expected failure): {description} - {ex.Message}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  FAIL: {description} - {ex.Message}");
                failed++;
            }
        }
    }

    // Test 1: Basic SQL Server to PostgreSQL conversion
    Test("Converts SQL Server to PostgreSQL correctly", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost;Database=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.ConnectionString.Contains("Host=localhost") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("Username=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 2: PostgreSQL to SQL Server conversion
    Test("Converts PostgreSQL to SQL Server correctly", () =>
    {
        var info = ConnectionStringConverter.Parse("Host=localhost;Database=test;Username=admin;Password=secret");
        info.Provider = "postgres";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "sqlserver");
        return result.ConnectionString.Contains("Server=localhost") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("User ID=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 3: SQL Server to MySQL conversion
    Test("Converts SQL Server to MySQL correctly", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost;Database=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "mysql");
        return result.ConnectionString.Contains("Server=localhost") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("Uid=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 4: Same provider conversion (should pass through)
    Test("Same provider conversion passes through unchanged", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost;Database=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "sqlserver");
        return result.ConnectionString == "Server=localhost;Database=test;User ID=admin;Password=secret";
    });

    // Test 5: Data Source alias mapping
    Test("Maps Data Source alias correctly in conversion", () =>
    {
        var info = ConnectionStringConverter.Parse("Data Source=localhost;Database=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.ConnectionString.Contains("Host=localhost") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("Username=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 6: Initial Catalog alias mapping
    Test("Maps Initial Catalog alias correctly in conversion", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost;Initial Catalog=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.ConnectionString.Contains("Host=localhost") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("Username=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 7: Port preservation in conversion
    Test("Preserves port in conversion", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost,5432;Database=test;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.ConnectionString.Contains("Host=localhost") &&
               result.ConnectionString.Contains("Port=5432") &&
               result.ConnectionString.Contains("Database=test") &&
               result.ConnectionString.Contains("Username=admin") &&
               result.ConnectionString.Contains("Password=secret");
    });

    // Test 8: Unmapped keys tracking
    Test("Tracks unmapped keys correctly", () =>
    {
        var info = ConnectionStringConverter.Parse("Server=localhost;Database=test;UnknownKey=value;User ID=admin;Password=secret");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.UnmappedKeys.Contains("UnknownKey");
    });

    // Test 9: Empty connection string handling
    Test("Handles empty connection string gracefully", () =>
    {
        var info = ConnectionStringConverter.Parse("");
        info.Provider = "sqlserver";
        var converter = new ConnectionStringConverter();
        var result = converter.Convert(info, "postgres");
        return result.ConnectionString == "";
    });

    // Test 10: Null provider handling
    Test("Throws exception for null/empty target provider", () =>
    {
        try
        {
            var info = ConnectionStringConverter.Parse("Server=localhost;Database=test");
            info.Provider = "sqlserver";
            var converter = new ConnectionStringConverter();
            var result = converter.Convert(info, "");
            return false; // Should not reach here
        }
        catch (ArgumentException)
        {
            return true; // Expected exception
        }
    });

    return expectFailure ? failed : passed;
}