# CLAUDE.md

## Overview
ConnStringDoctor: .NET 10 CLI + library that parses, diagnoses (reachability, TLS, pooling, timeouts, deprecated/unknown keys), normalizes, converts and compares database connection strings.

## Build
```bash
dotnet build conn-string-doctor.csproj
dotnet run -- diagnose --connection-string "Server=localhost;Database=test;" --format text
dotnet run -- list-checks
```
CLI commands (System.CommandLine): `diagnose`, `compare`, `normalize`, `list-checks`, `selftest`. Output formats: text, markdown, html, json.

## Tests
```bash
dotnet test tests/conn-string-doctor.tests.csproj
dotnet test tests/conn-string-doctor.tests.csproj --filter "FullyQualifiedName~PoolConfigCheckTests"
```
xUnit 2.9. Tests live in `tests/` as a separate project referencing the root csproj; the root csproj explicitly excludes `tests/**` and `build/**` from compilation.

## Lint / Format
No analyzers or formatter config. `Nullable` and `ImplicitUsings` enabled, `LangVersion=latest`. Use `dotnet format` if needed.

## Key directories and entry points
- `Program.cs` - CLI entry point (top-level statements, wires all commands and options).
- `src/` - library code, namespace `ConnStringDoctor`.
  - `ConnectionStringParser.cs` / `ConnectionStringTokenizer.cs` - parsing into `ConnectionStringInfo`.
  - `IDiagnosticCheck.cs` - check contract (`Name`, `Description`, `RunAsync(ConnectionStringInfo, CancellationToken)` -> `DiagnosticResult`).
  - `*Check.cs` (`PoolConfigCheck`, `TimeoutConfigCheck`, `DnsAndTcpCheck`, `DuplicateKeyCheck`, `DeprecatedKeywordCheck`, `UnknownKeywordCheck`, `TimeoutSanityCheck`), `TlsInspector.cs` - diagnostic checks.
  - `DiagnosticResult*.cs` - result model plus Markdown/HTML renderers.
  - `ConnectionStringConverter*.cs`, `ConnectionStringNormalizer.cs`, `ConnectionStringComparator.cs`, `ConnectionStringRedactor.cs` - conversion, normalization, comparison, redaction.
  - `FluentConnectionStringBuilder*.cs` - fluent builder API.
  - `ProviderDetector.cs`, `ProviderRegistry.cs`, `DbProvider.cs` - provider detection.
- `tests/` - xUnit tests, namespace `ConnStringDoctor.Tests`.
- `docs/` - per-class Markdown API docs.
- `build/` - committed published binaries (do not edit by hand).
- `TLS_INSPECTOR_IMPLEMENTATION.md` - design notes for TLS inspection.

## Conventions
- One type per file, file name equals type name. Checks end in `Check`, extension classes in `Extensions`, JSON helpers in `JsonExtensions`, validation helpers in `Validation`.
- Types are `internal sealed` by default; tests access them via `InternalsVisibleTo` in `src/AssemblyInfo.cs`.
- Every file starts with `#nullable enable`; XML doc comments on public/internal members (`GenerateDocumentationFile=true`).
- New diagnostic check: implement `IDiagnosticCheck` in `src/<Name>Check.cs`, register it in `Program.cs`, add `tests/<Name>CheckTests.cs`.
- Test naming: `Scenario_Condition_ExpectedResult`, `[Fact]`, `Assert.*` from xUnit. Never put test code under root project sources.
- Credentials are redacted by default in all outputs; keep that behavior.
- Stray files in the repo root (`}`, `Example usage`, `Console.WriteLine(...)`) are accidental artifacts; ignore them, do not depend on them.
