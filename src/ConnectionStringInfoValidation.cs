namespace ConnStringDoctor
{
    /// <summary>
    /// Provides validation methods for <see cref="ConnectionStringInfo"/> instances.
    /// </summary>
    /// <remarks>
    /// Server, port, and database are checked with the same per-field rules as
    /// <see cref="FluentConnectionStringBuilderValidation"/>. The following differences are intentional:
    /// <list type="bullet">
    /// <item><description>The user is always required here. <see cref="ConnectionStringInfo"/> does not model integrated security,
    /// so a parsed Windows-authentication string without a user is reported; the builder only requires a user when integrated security is off.</description></item>
    /// <item><description>The password is not checked here, because parsed connection strings may legitimately omit it.
    /// The builder requires one whenever integrated security is off.</description></item>
    /// <item><description>Pooling and timeout values are not checked here, because <see cref="ConnectionStringInfo"/> keeps them in
    /// <see cref="ConnectionStringInfo.Properties"/>. Use <c>PoolConfigCheck</c> and <c>TimeoutSanityCheck</c> for those.</description></item>
    /// <item><description>Provider validity is checked against <see cref="DbProvider.Unknown"/>, not against a provider name string.</description></item>
    /// </list>
    /// </remarks>
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

            var problems = new List<string?>
            {
                value.Provider == DbProvider.Unknown ? "Provider is Unknown" : null,
                ConnectionStringFieldValidation.ServerProblem(value.Server),
                ConnectionStringFieldValidation.PortProblem(value.Port),
                ConnectionStringFieldValidation.DatabaseProblem(value.Database),
                ConnectionStringFieldValidation.UserProblem(value.User),
            };

            if (value.Properties is not null)
            {
                problems.AddRange(value.Properties
                    .Where(property => string.IsNullOrEmpty(property.Key) || string.IsNullOrEmpty(property.Value))
                    .Select(property => $"Property key or value is null or empty: {property.Key}={property.Value}"));
            }

            return problems.OfType<string>().ToList().AsReadOnly();
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
                throw new ArgumentException($"Invalid ConnectionStringInfo: {string.Join(", ", problems)}", nameof(value));
            }
        }
    }
}
