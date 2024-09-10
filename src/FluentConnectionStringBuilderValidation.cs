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

            // Unfortunately, we cannot access the internal state of FluentConnectionStringBuilder
            // to perform a thorough validation. We can only check if the Build method throws an exception.
            try
            {
                value.Build();
            }
            catch (Exception ex)
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
                throw new ArgumentException(string.Join(Environment.NewLine, problems), nameof(value));
            }
        }
    }
}
