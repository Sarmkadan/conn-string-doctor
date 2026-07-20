using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides utilities for redacting sensitive information from database connection strings.
    /// </summary>
    public static class ConnectionStringRedactor
    {
        // Keys that are considered to hold secrets. The check is case-insensitive.
        private static readonly string[] SecretKeyPatterns =
        {
            "Password",
            "Pwd",
            "User Id",
            "UserID",
            "User",
            "Token",
            "AccessToken",
            "Secret"
        };

        /// <summary>
        /// Determines whether the specified key is considered a secret key.
        /// </summary>
        private static bool IsSecretKey(string key)
        {
            return SecretKeyPatterns.Any(pattern =>
                key.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Determines whether the specified key is a password key.
        /// </summary>
        private static bool IsPasswordKey(string key)
        {
            return key.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   key.IndexOf("Pwd", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Redacts all secret values in the supplied connection string.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <param name="mask">The mask to replace secret values with.</param>
        /// <returns>The redacted connection string.</returns>
        public static string Redact(string connectionString, string mask = "***")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            try
            {
                var builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                foreach (string key in builder.Keys.Cast<string>())
                {
                    if (IsSecretKey(key))
                    {
                        builder[key] = mask;
                    }
                }

                return builder.ConnectionString;
            }
            catch (ArgumentException)
            {
                // If parsing fails, return the original string unchanged.
                return connectionString;
            }
        }

        /// <summary>
        /// Redacts all secret values in the supplied connection string and returns them as a dictionary.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <param name="mask">The mask to replace secret values with.</param>
        /// <returns>A dictionary of keyword->value with sensitive values masked.</returns>
        public static IReadOnlyDictionary<string, string> RedactToDictionary(string connectionString, string mask = "***")
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(connectionString))
                return result;

            try
            {
                var builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                foreach (string key in builder.Keys.Cast<string>())
                {
                    var value = builder[key]?.ToString();
                    if (IsSecretKey(key))
                    {
                        result[key] = mask;
                    }
                    else
                    {
                        result[key] = value ?? string.Empty;
                    }
                }
            }
            catch (ArgumentException)
            {
                // If parsing fails, return empty dictionary.
            }
            return result;
        }

        /// <summary>
        /// Redacts only the password value in the supplied connection string.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <returns>The connection string with the password redacted.</returns>
        public static string RedactKeepUser(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            try
            {
                var builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                foreach (string key in builder.Keys.Cast<string>())
                {
                    if (IsPasswordKey(key))
                    {
                        builder[key] = "***";
                    }
                }

                return builder.ConnectionString;
            }
            catch (ArgumentException)
            {
                return connectionString;
            }
        }

        /// <summary>
        /// Checks whether the supplied connection string contains any secret keys.
        /// </summary>
        /// <param name="connectionString">The connection string to inspect.</param>
        /// <returns>True if any secret key is present; otherwise, false.</returns>
        public static bool ContainsSecrets(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                var builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                foreach (string key in builder.Keys.Cast<string>())
                {
                    if (IsSecretKey(key))
                        return true;
                }

                return false;
            }
            catch (ArgumentException)
            {
                // If parsing fails, assume no secrets were detected.
                return false;
            }
        }
    }
}
