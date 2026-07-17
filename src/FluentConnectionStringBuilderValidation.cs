namespace ConnStringDoctor
{
    /// <summary>
    /// Provides validation helpers for the <see cref="FluentConnectionStringBuilder"/> type.
    /// </summary>
    public static class FluentConnectionStringBuilderValidation
    {
        /// <summary>
        /// Validates the specified <paramref name="value"/> and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The <see cref="FluentConnectionStringBuilder"/> instance to validate.</param>
        /// <returns>A list of human-readable problems, or an empty list if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this FluentConnectionStringBuilder value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate provider
            if (string.IsNullOrWhiteSpace(value._provider))
            {
                problems.Add("Provider cannot be null or whitespace.");
            }

            // Validate required fields based on provider type
            switch (value._provider?.ToLowerInvariant())
            {
                case "sqlite" or "sqlitepclraw":
                    if (string.IsNullOrWhiteSpace(value._database))
                    {
                        problems.Add("SQLite connection string requires a database path.");
                    }
                    break;
            }

            // Validate credentials consistency
            if (!value._integratedSecurity && string.IsNullOrWhiteSpace(value._user))
            {
                problems.Add("User credentials must be provided when integrated security is disabled.");
            }

            if (!value._integratedSecurity && string.IsNullOrWhiteSpace(value._password))
            {
                problems.Add("Password must be provided when user credentials are configured.");
            }

            // Validate pooling configuration
            if (value._poolingMin.HasValue && value._poolingMax.HasValue && value._poolingMin.Value < 0)
            {
                problems.Add("Minimum pool size cannot be negative.");
            }

            if (value._poolingMin.HasValue && value._poolingMax.HasValue && value._poolingMax.Value < value._poolingMin.Value)
            {
                problems.Add("Maximum pool size cannot be less than minimum pool size.");
            }

            // Validate timeout
            if (value._timeout.HasValue && value._timeout.Value <= 0)
            {
                problems.Add("Timeout must be a positive integer.");
            }

            // Validate port if specified
            if (value._port.HasValue && value._port.Value <= 0)
            {
                problems.Add("Port must be a positive integer.");
            }

            // Final validation: attempt to build and catch any build-time exceptions
            try
            {
                value.Build();
            }
            catch (Exception ex) when (ex is not ArgumentException and not ArgumentNullException)
            {
                problems.Add(ex.Message);
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="value"/> is valid.
        /// </summary>
        /// <param name="value">The <see cref="FluentConnectionStringBuilder"/> instance to validate.</param>
        /// <returns>true if the instance is valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this FluentConnectionStringBuilder value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <paramref name="value"/> is valid, throwing an exception if it is not.
        /// </summary>
        /// <param name="value">The <see cref="FluentConnectionStringBuilder"/> instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static void EnsureValid(this FluentConnectionStringBuilder value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();

            if (problems.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, problems), nameof(value));
            }
        }
    }
}
