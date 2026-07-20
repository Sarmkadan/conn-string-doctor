using System.CommandLine;
using ConnStringDoctor;

var rootCommand = new RootCommand("Connection String Doctor - Diagnose connection strings for common issues");

// Define options
var connectionStringOption = new Option<string>(
    name: "--connection-string",
    description: "The connection string to diagnose");

var formatOption = new Option<string>(
    name: "--format",
    description: "Output format (text, markdown)",
    getDefaultValue: () => "text");

var outputOption = new Option<FileInfo?>
    (name: "--output",
    description: "Output file path (optional)");

var includeSuccessOption = new Option<bool>(
    name: "--include-success",
    description: "Include successful checks in output",
    getDefaultValue: () => false);

var failOnOption = new Option<Severity>(
    name: "--fail-on",
    description: "Exit with non-zero code if any diagnostic meets or exceeds this severity level (Info, Warning, Error)",
    getDefaultValue: () => Severity.Info);

rootCommand.AddOption(connectionStringOption);
rootCommand.AddOption(formatOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(includeSuccessOption);
rootCommand.AddOption(failOnOption);

rootCommand.SetHandler(async (context) =>
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

return await rootCommand.InvokeAsync(args);

static string FormatAsText(List<DiagnosticResult> results, bool includeSuccess)
{
    var builder = new System.Text.StringBuilder();

    foreach (var result in results.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
    {
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
