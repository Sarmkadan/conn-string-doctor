namespace ConnStringDoctor
{
    /// <summary>
    /// Provides validation methods for <see cref="ConnectionStringInfo"/> instances.
    /// </summary>
    public static class ConnectionStringInfoValidation
    {
        /// <summary>
        /// Validates the provided <see cref="ConnectionStringInfo"/> instance.
        /// </summary>
        /// <param name="value">The <see cref="ConnectionStringInfo"/> instance to validate.</param>
        /// <returns>A list of human-readable problems with the instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this ConnectionStringInfo value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.Provider == DbProvider.Unknown)
            {
                problems.Add("Provider is Unknown");
            }

            if (string.IsNullOrEmpty(value.Server))
            {
                problems.Add("Server is null or empty");
            }

            if (value.Port.HasValue && (value.Port is < 1 or > 65535))
            {
                problems.Add("Port is out of range (1-65535)");
            }

            if (string.IsNullOrEmpty(value.Database))
            {
                problems.Add("Database is null or empty");
            }

            if (string.IsNullOrEmpty(value.User))
            {
                problems.Add("User is null or empty");
            }

            if (value.Properties is not null)
            {
                foreach (var property in value.Properties)
                {
                    if (string.IsNullOrEmpty(property.Key) || string.IsNullOrEmpty(property.Value))
                    {
                        problems.Add($"Property key or value is null or empty: {property.Key}={property.Value}");
                    }
                }
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Checks if the provided <see cref="ConnectionStringInfo"/> instance is valid.
        /// </summary>
        /// <param name="value">The <see cref="ConnectionStringInfo"/> instance to check.</param>
        /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this ConnectionStringInfo value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures the provided <see cref="ConnectionStringInfo"/> instance is valid.
        /// </summary>
        /// <param name="value">The <see cref="ConnectionStringInfo"/> instance to ensure.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if the instance is invalid.</exception>
        public static void EnsureValid(this ConnectionStringInfo value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);

            if (problems.Count > 0)
            {
                throw new ArgumentException($"Invalid ConnectionStringInfo instance: {string.Join(", ", problems)}");
            }
        }
    }
}
