namespace ConnStringDoctor
{
    /// <summary>
    /// Provides validation helpers for the <see cref="FluentConnectionStringBuilder"/> type.
    /// </summary>
    /// <remarks>
    /// Server, port, database, credential, and pool-size rules are shared with <see cref="ConnectionStringInfoValidation"/>.
    /// The builder differs from parsed connection strings in these intentional ways:
    /// <list type="bullet">
    /// <item><description>Credentials and database are required only when integrated security is off. SQLite always requires a database path.</description></item>
    /// <item><description>A password is required when integrated security is off. <see cref="ConnectionStringInfoValidation"/> does not check passwords.</description></item>
    /// <item><description>Pool sizes and timeout are checked, because the builder models them as typed fields.</description></item>
    /// <item><description>Setter methods such as <c>WithHost</c> and <c>WithTimeout</c> reject empty or non-positive input eagerly.
    /// Validation reports the same conditions as problems in the in-progress state, so a builder can be inspected before the state is complete.
    /// Out-of-range port numbers are reported here rather than thrown by <c>WithHost</c>.</description></item>
    /// <item><description>Build-time failures from <see cref="FluentConnectionStringBuilder.Build"/> are reported as problems, except argument exceptions, which propagate.</description></item>
    /// </list>
    /// </remarks>
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

            var isSqlite = value._provider?.ToLowerInvariant() is "sqlite" or "sqlitepclraw";
            var problems = new List<string?>
            {
                string.IsNullOrWhiteSpace(value._provider) ? "Provider cannot be null or whitespace." : null,
                ConnectionStringFieldValidation.ServerProblem(value._host),
                ConnectionStringFieldValidation.PortProblem(value._port),
                ConnectionStringFieldValidation.DatabaseProblem(value._database, required: !value._integratedSecurity || isSqlite),
                ConnectionStringFieldValidation.UserProblem(value._user, required: !value._integratedSecurity),
                ConnectionStringFieldValidation.PasswordProblem(value._password, required: !value._integratedSecurity),
                value._integratedSecurity && (!string.IsNullOrEmpty(value._user) || !string.IsNullOrEmpty(value._password))
                    ? "Conflicting options: integrated security is enabled but credentials are also configured."
                    : null,
                ConnectionStringFieldValidation.MinPoolSizeProblem(value._poolingMin),
                ConnectionStringFieldValidation.PoolSizeRangeProblem(value._poolingMin, value._poolingMax),
                ConnectionStringFieldValidation.TimeoutProblem(value._timeout),
            };

            var result = problems.OfType<string>().ToList();

            try
            {
                value.Build();
            }
            catch (Exception ex) when (ex is not ArgumentException and not ArgumentNullException)
            {
                result.Add(ex.Message);
            }

            return result;
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
            return Validate(value).Count == 0;
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

            var problems = Validate(value);

            if (problems.Count > 0)
            {
                throw new ArgumentException($"Invalid FluentConnectionStringBuilder: {string.Join(", ", problems)}", nameof(value));
            }
        }
    }
}
