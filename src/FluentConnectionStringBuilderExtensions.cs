using System;
using System.Collections.Generic;

#nullable enable

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides convenience extension methods for <see cref="FluentConnectionStringBuilder"/>.
    /// </summary>
    public static class FluentConnectionStringBuilderExtensions
    {
        /// <summary>
        /// Adds multiple options to the connection string builder in a single call.
        /// </summary>
        /// <param name="builder">The connection string builder.</param>
        /// <param name="options">Dictionary of key-value pairs to add.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public static FluentConnectionStringBuilder WithOptions(this FluentConnectionStringBuilder builder, Dictionary<string, string> options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            foreach (var option in options)
            {
                builder.WithOption(option.Key, option.Value);
            }

            return builder;
        }

        /// <summary>
        /// Sets the connection timeout to a specific time span.
        /// </summary>
        /// <param name="builder">The connection string builder.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is not positive or exceeds maximum allowed value.</exception>
        public static FluentConnectionStringBuilder WithTimeout(this FluentConnectionStringBuilder builder, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (timeout.TotalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
            }

            if (timeout.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout exceeds maximum allowed value.");
            }

            // Round up so sub-second timeouts do not truncate to zero.
            return builder.WithTimeout((int)Math.Ceiling(timeout.TotalSeconds));
        }

        /// <summary>
        /// Sets the pooling configuration with a single timeout value for both min and max pool size.
        /// </summary>
        /// <param name="builder">The connection string builder.</param>
        /// <param name="poolSize">The pool size (min and max will be set to this value).</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="poolSize"/> is not positive.</exception>
        public static FluentConnectionStringBuilder WithPoolSize(this FluentConnectionStringBuilder builder, int poolSize)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (poolSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be positive.");
            }

            return builder.WithPooling(poolSize, poolSize);
        }

        /// <summary>
        /// Sets the database name from a connection string URI format (e.g., "Server=localhost;Database=mydb").
        /// </summary>
        /// <param name="builder">The connection string builder.</param>
        /// <param name="databaseName">The database name to extract from the URI or use directly.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="databaseName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static FluentConnectionStringBuilder WithDatabaseFromUri(this FluentConnectionStringBuilder builder, string databaseName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

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