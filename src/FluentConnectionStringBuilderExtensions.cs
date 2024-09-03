using System;
using System.Collections.Generic;

#nullable enable

namespace ConnStringDoctor
{
    public static class FluentConnectionStringBuilderExtensions
    {
        /// <summary>
        /// Adds multiple options to the connection string builder in a single call.
        /// </summary>
        /// <param name="builder">The connection string builder</param>
        /// <param name="options">Dictionary of key-value pairs to add</param>
        /// <returns>The same builder instance for fluent chaining</returns>
        public static FluentConnectionStringBuilder WithOptions(this FluentConnectionStringBuilder builder, Dictionary<string, string> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var option in options)
            {
                builder.WithOption(option.Key, option.Value);
            }

            return builder;
        }

        /// <summary>
        /// Sets the connection timeout to a specific time span.
        /// </summary>
        /// <param name="builder">The connection string builder</param>
        /// <param name="timeout">The timeout value</param>
        /// <returns>The same builder instance for fluent chaining</returns>
        public static FluentConnectionStringBuilder WithTimeout(this FluentConnectionStringBuilder builder, TimeSpan timeout)
        {
            if (timeout.TotalSeconds <= 0)
            {
                throw new ArgumentException("Timeout must be positive.", nameof(timeout));
            }

            if (timeout.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentException("Timeout exceeds maximum allowed value.", nameof(timeout));
            }

            return builder.WithTimeout((int)timeout.TotalSeconds);
        }

        /// <summary>
        /// Sets the pooling configuration with a single timeout value for both min and max pool size.
        /// </summary>
        /// <param name="builder">The connection string builder</param>
        /// <param name="poolSize">The pool size (min and max will be set to this value)</param>
        /// <returns>The same builder instance for fluent chaining</returns>
        public static FluentConnectionStringBuilder WithPoolSize(this FluentConnectionStringBuilder builder, int poolSize)
        {
            if (poolSize <= 0)
            {
                throw new ArgumentException("Pool size must be positive.", nameof(poolSize));
            }

            return builder.WithPooling(poolSize, poolSize);
        }

        /// <summary>
        /// Sets the database name from a connection string URI format (e.g., "Server=localhost;Database=mydb").
        /// </summary>
        /// <param name="builder">The connection string builder</param>
        /// <param name="databaseName">The database name to extract from the URI</param>
        /// <returns>The same builder instance for fluent chaining</returns>
        public static FluentConnectionStringBuilder WithDatabaseFromUri(this FluentConnectionStringBuilder builder, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name cannot be null or whitespace.", nameof(databaseName));
            }

            // Extract database name from URI format (e.g., "mydb" from "Server=localhost;Database=mydb")
            var parts = databaseName.Split(new[] { ';', '=' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(parts[i + 1]))
                {
                    return builder.WithDatabase(parts[i + 1].Trim());
                }
            }

            return builder.WithDatabase(databaseName.Trim());
        }
    }
}