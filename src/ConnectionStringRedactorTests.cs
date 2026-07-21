using System;
using System.Collections.Generic;

namespace ConnStringDoctor
{
    /// <summary>
    /// Tests for ConnectionStringRedactor.
    /// </summary>
    public static class ConnectionStringRedactorTests
    {
        /// <summary>
        /// Runs all tests for ConnectionStringRedactor.
        /// </summary>
        public static void RunTests()
        {
            TestFullRedaction();
            TestPartialRedaction();
            TestRedactToDictionaryWithPartialMode();
            TestRedactKeepUser();
            TestContainsSecrets();
            Console.WriteLine("All ConnectionStringRedactor tests passed");
        }

        /// <summary>
        /// Tests the Full redaction mode (default behavior).
        /// </summary>
        private static void TestFullRedaction()
        {
            var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;Token=abc123";

            // Test with default Full mode
            var resultFull = ConnectionStringRedactor.Redact(connectionString);
            if (resultFull.Contains("myPassword")) throw new Exception("Password not masked with Full mode");
            if (resultFull.Contains("abc123")) throw new Exception("Token not masked with Full mode");

            // Test with explicit Full mode
            var resultExplicit = ConnectionStringRedactor.Redact(connectionString, RedactionMode.Full);
            if (resultExplicit != resultFull) throw new Exception("Explicit Full mode differs from default");

            Console.WriteLine("Full redaction tests passed");
        }

        /// <summary>
        /// Tests the Partial redaction mode.
        /// </summary>
        private static void TestPartialRedaction()
        {
            var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;Token=abc123";

            var result = ConnectionStringRedactor.Redact(connectionString, RedactionMode.Partial);

            // Check that secrets are partially redacted
            if (result.Contains("myPassword")) throw new Exception("Password not redacted with Partial mode");
            if (result.Contains("myUser")) throw new Exception("User Id not redacted with Partial mode");
            if (result.Contains("abc123")) throw new Exception("Token not redacted with Partial mode");

            // Check that non-secrets are preserved
            if (!result.Contains("Server=myServer", StringComparison.OrdinalIgnoreCase)) throw new Exception("Server not preserved with Partial mode");

            // Check partial redaction pattern: first 2 and last 2 chars visible
            if (!result.Contains("my****rd")) throw new Exception("Password not partially redacted correctly");
            if (!result.Contains("ab****23")) throw new Exception("Token not partially redacted correctly");

            Console.WriteLine("Partial redaction tests passed");
        }

        /// <summary>
        /// Tests RedactToDictionary with Partial mode.
        /// </summary>
        private static void TestRedactToDictionaryWithPartialMode()
        {
            var connectionString = "Server=myServer;Password=myPassword;User Id=myUser";
            var result = ConnectionStringRedactor.RedactToDictionary(connectionString, RedactionMode.Partial);

            if (result.GetValueOrDefault("password") == "myPassword" || result.GetValueOrDefault("Password") == "myPassword") throw new Exception("Password not redacted in dictionary");
            var redactedPassword = result.GetValueOrDefault("password") ?? result.GetValueOrDefault("Password");
            if (redactedPassword != "my****rd") throw new Exception($"Password not partially redacted correctly in dictionary. Got: {redactedPassword}");
            if (result.GetValueOrDefault("server") != "myServer" && result.GetValueOrDefault("Server") != "myServer") throw new Exception("Server not preserved in dictionary");
            var redactedUserId = result.GetValueOrDefault("user id") ?? result.GetValueOrDefault("User Id");
            if (redactedUserId != "my****er") throw new Exception($"User Id not partially redacted correctly in dictionary. Got: {redactedUserId}");

            Console.WriteLine("RedactToDictionary with Partial mode tests passed");
        }

        /// <summary>
        /// Tests RedactKeepUser method.
        /// </summary>
        private static void TestRedactKeepUser()
        {
            var connectionString = "Server=myServer;Password=myPassword;User Id=myUser";
            var result = ConnectionStringRedactor.RedactKeepUser(connectionString);

            if (result.Contains("myPassword")) throw new Exception("Password not masked by RedactKeepUser");
            if (!result.Contains("myUser")) throw new Exception("User not preserved by RedactKeepUser");
            if (result.Contains("Password")) throw new Exception("Password still visible in result");
            if (!result.Contains("Server")) throw new Exception("Server not preserved by RedactKeepUser");

            Console.WriteLine("RedactKeepUser tests passed");
        }

        /// <summary>
        /// Tests ContainsSecrets method.
        /// </summary>
        private static void TestContainsSecrets()
        {
            var withSecrets = "Server=myServer;Password=myPassword";
            var withoutSecrets = "Server=myServer;Database=mydb";

            if (!ConnectionStringRedactor.ContainsSecrets(withSecrets))
                throw new Exception("ContainsSecrets failed to detect secrets");

            if (ConnectionStringRedactor.ContainsSecrets(withoutSecrets))
                throw new Exception("ContainsSecrets incorrectly detected secrets");

            if (ConnectionStringRedactor.ContainsSecrets(null))
                throw new Exception("ContainsSecrets failed on null input");

            if (ConnectionStringRedactor.ContainsSecrets(""))
                throw new Exception("ContainsSecrets failed on empty string");

            Console.WriteLine("ContainsSecrets tests passed");
        }
    }
}