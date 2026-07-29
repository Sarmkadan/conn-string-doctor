using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace ConnStringDoctor
{
    /// <summary>
    /// Specifies the redaction mode for sensitive values in connection strings.
    /// </summary>
    public enum RedactionMode
    {
        /// <summary>
        /// Completely masks the value with the specified mask string.
        /// </summary>
        Full,

        /// <summary>
        /// Partially masks the value, keeping the first 2 and last 2 characters visible.
        /// For example: "password123" becomes "pa****rd"
        /// </summary>
        Partial
    }

    /// <summary>
    /// Options that control how redaction is performed.
    /// </summary>
    public sealed class RedactionOptions
    {
        /// <summary>
        /// The mask string used when <see cref="RedactionMode.Full"/> is selected.
        /// Defaults to "****".
        /// </summary>
        public string Mask { get; set; } = "****";

        /// <summary>
        /// When true, the redacted value will keep the original length (by padding the mask).
        /// This flag is currently not used by the existing logic but is provided for future extensions.
        /// </summary>
        public bool KeepLength { get; set; } = false;

        /// <summary>
        /// Collection of key patterns that are considered sensitive.
        /// The default list mirrors the previous hard‑coded list.
        /// </summary>
        public IReadOnlyCollection<string> SensitiveKeyPatterns { get; set; } = new List<string>
        {
            "Password",
            "Pwd",
            "Passwd",
            "User Id",
            "UserID",
            "User",
            "Token",
            "AccessToken",
            "Secret"
        };
    }

    /// <summary>
    /// Provides utilities for redacting sensitive information from database connection strings.
    /// </summary>
    public static class ConnectionStringRedactor
    {
        // Default options – used by the original overloads to preserve backward compatibility.
        private static readonly RedactionOptions DefaultOptions = new RedactionOptions();

        /// <summary>
        /// Redacts all secret values in the supplied connection string.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <param name="mode">The redaction mode to use.</param>
        /// <param name="mask">The mask to use when mode is Full.</param>
        /// <returns>The redacted connection string.</returns>
        public static string Redact(string connectionString, RedactionMode mode = RedactionMode.Full, string mask = "****")
        {
            // Preserve the original API surface while delegating to the options‑based implementation.
            var options = new RedactionOptions { Mask = mask };
            return Redact(connectionString, options, mode);
        }

        /// <summary>
        /// Redacts all secret values in the supplied connection string using the supplied options.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <param name="options">Redaction options that control masking and key detection.</param>
        /// <param name="mode">The redaction mode to use.</param>
        /// <returns>The redacted connection string.</returns>
        public static string Redact(string connectionString, RedactionOptions options, RedactionMode mode = RedactionMode.Full)
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
                    if (IsSecretKey(key, options))
                    {
                        var value = builder[key]?.ToString();
                        builder[key] = RedactValue(value, mode, options);
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
        /// <param name="mode">The redaction mode to use.</param>
        /// <param name="mask">The mask to use when mode is Full.</param>
        /// <returns>A dictionary of keyword-&gt;value with sensitive values redacted.</returns>
        public static IReadOnlyDictionary<string, string> RedactToDictionary(string connectionString, RedactionMode mode = RedactionMode.Full, string mask = "****")
        {
            var options = new RedactionOptions { Mask = mask };
            return RedactToDictionary(connectionString, options, mode);
        }

        /// <summary>
        /// Redacts all secret values in the supplied connection string and returns them as a dictionary,
        /// using the supplied options.
        /// </summary>
        /// <param name="connectionString">The original connection string.</param>
        /// <param name="options">Redaction options that control masking and key detection.</param>
        /// <param name="mode">The redaction mode to use.</param>
        /// <returns>A dictionary of keyword-&gt;value with sensitive values redacted.</returns>
        public static IReadOnlyDictionary<string, string> RedactToDictionary(string connectionString, RedactionOptions options, RedactionMode mode = RedactionMode.Full)
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
                    if (IsSecretKey(key, options))
                    {
                        result[key] = RedactValue(value, mode, options);
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
            return ContainsSecrets(connectionString, DefaultOptions);
        }

        /// <summary>
        /// Checks whether the supplied connection string contains any secret keys,
        /// using the supplied options.
        /// </summary>
        /// <param name="connectionString">The connection string to inspect.</param>
        /// <param name="options">Redaction options that control which keys are considered secret.</param>
        /// <returns>True if any secret key is present; otherwise, false.</returns>
        public static bool ContainsSecrets(string connectionString, RedactionOptions options)
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
                    if (IsSecretKey(key, options))
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

        /// <summary>
        /// Determines whether the specified key is considered a secret key, using the supplied options.
        /// </summary>
        private static bool IsSecretKey(string key, RedactionOptions options)
        {
            return options.SensitiveKeyPatterns.Any(pattern =>
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
        /// Redacts a single value according to the specified mode and options.
        /// </summary>
        private static string RedactValue(string value, RedactionMode mode, RedactionOptions options)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return mode switch
            {
                RedactionMode.Full => options.Mask,
                RedactionMode.Partial => ApplyPartialRedaction(value, options.Mask),
                _ => options.Mask
            };
        }

        /// <summary>
        /// Applies partial redaction to a value, keeping first 2 and last 2 characters visible.
        /// </summary>
        /// <param name="value">The value to redact.</param>
        /// <param name="mask">The mask string to use between the visible characters.</param>
        /// <returns>The partially redacted value.</returns>
        private static string ApplyPartialRedaction(string value, string mask)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 4)
                return mask;

            var first2 = value[..2];
            var last2 = value[^2..];
            return $"{first2}{mask}{last2}";
        }
    }
}
