using System.CommandLine;
using ConnStringDoctor;

var rootCommand = new RootCommand("Connection String Doctor - Diagnoses connection strings for common issues and compares them");

// Define options for diagnose command
var connectionStringOption = new Option<string>(
    name: "--connection-string",
    description: "The connection string to diagnose");

var formatOption = new Option<string>(
    name: "--format",
    description: "Output format (text, markdown)",
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
    description: "Exit with non-zero code if any diagnostic meets or exceeds this severity level (Info, Warning, Error)",
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
        var connectionString = context.ParseResult.GetValueForOption(connectionStringOption);
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
            new DuplicateKeyCheck(),
            new PoolConfigCheck(),
            new TimeoutConfigCheck(),
            new TimeoutSanityCheck(),
            new DnsAndTcpCheck()
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
        if (format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
        {
            output = DiagnosticResultMarkdownRenderer.Render(results, includeSuccess);
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
        var maxSeverity = results.Max(r => r.ResultSeverity);
        if (maxSeverity >= failOnSeverity)
        {
            Environment.Exit(1);
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

// Add subcommands to root
rootCommand.AddCommand(diagnoseCommand);
rootCommand.AddCommand(compareCommand);

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
