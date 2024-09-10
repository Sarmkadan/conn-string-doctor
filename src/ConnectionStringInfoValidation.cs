namespace ConnStringDoctor
{
    public static class ConnectionStringInfoValidation
    {
        /// <summary>
        /// Validates the provided ConnectionStringInfo instance.
        /// </summary>
        /// <param name="value">The ConnectionStringInfo instance to validate.</param>
        /// <returns>A list of human-readable problems with the instance.</returns>
        public static IReadOnlyList<string> Validate(this ConnectionStringInfo value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.Provider == null)
            {
                problems.Add("Provider is null");
            }

            if (string.IsNullOrEmpty(value.Server))
            {
                problems.Add("Server is null or empty");
            }

            if (value.Port.HasValue && (value.Port < 1 || value.Port > 65535))
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

            if (value.Properties != null)
            {
                foreach (var property in value.Properties)
                {
                    if (string.IsNullOrEmpty(property.Key) || string.IsNullOrEmpty(property.Value))
                    {
                        problems.Add($"Property key or value is null or empty: {property.Key}={property.Value}");
                    }
                }
            }

            return problems;
        }

        /// <summary>
        /// Checks if the provided ConnectionStringInfo instance is valid.
        /// </summary>
        /// <param name="value">The ConnectionStringInfo instance to check.</param>
        /// <returns>True if the instance is valid, false otherwise.</returns>
        public static bool IsValid(this ConnectionStringInfo value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures the provided ConnectionStringInfo instance is valid.
        /// </summary>
        /// <param name="value">The ConnectionStringInfo instance to ensure.</param>
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
