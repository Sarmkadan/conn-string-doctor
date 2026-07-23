using System.Runtime.CompilerServices;

// Grants the dedicated test project visibility into internal diagnostic check types
// (e.g. PoolConfigCheck) so tests can exercise them directly without making them part
// of the shipped package's public API surface.
[assembly: InternalsVisibleTo("conn-string-doctor.tests")]
