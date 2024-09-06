using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace ConnStringDoctor
{
    /// <summary>
    /// Builds provider-specific connection strings using a fluent API.
    /// </summary>
    public sealed class FluentConnectionStringBuilder
    {
        private static readonly char[] _charsRequiringQuotes = { ';', '=', '"', '\'' };

        private readonly string _provider;
        private string? _host;
        private int? _port;
        private string? _database;
        private string? _user;
        private string? _password;
        private bool _integratedSecurity;
        private bool _sslRequired = true;
        private int? _poolingMin;
        private int? _poolingMax;
        private int? _timeout;
        private readonly Dictionary<string, string> _options = new();

        /// <summary>
        /// Creates a builder for the specified provider (e.g. "sqlserver", "postgresql", "mysql", "sqlite").
        /// </summary>
        /// <param name="provider">The provider name; unrecognized providers produce a generic key=value string.</param>
        /// <returns>A new builder instance.</returns>
        /// <exception cref="ArgumentException"><paramref name="provider"/> is null, empty, or whitespace.</exception>
        public static FluentConnectionStringBuilder For(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentException("Provider cannot be null or whitespace.", nameof(provider));
            }

            return new FluentConnectionStringBuilder(provider);
        }

        private FluentConnectionStringBuilder(string provider)
        {
            _provider = provider.Trim();
        }

        /// <summary>
        /// Sets the host (and optionally the port) to connect to.
        /// </summary>
        /// <param name="host">The server host name or address.</param>
        /// <param name="port">The optional port number.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="host"/> is null, empty, or whitespace.</exception>
        public FluentConnectionStringBuilder WithHost(string host, int? port = null)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host cannot be null or whitespace.", nameof(host));
            }

            _host = host.Trim();
            _port = port;
            return this;
        }

        /// <summary>
        /// Sets the database name (or file path for SQLite).
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="db"/> is null, empty, or whitespace.</exception>
        public FluentConnectionStringBuilder WithDatabase(string db)
        {
            if (string.IsNullOrWhiteSpace(db))
            {
                throw new ArgumentException("Database cannot be null or whitespace.", nameof(db));
            }

            _database = db.Trim();
            return this;
        }

        /// <summary>
        /// Sets user/password credentials and disables integrated security.
        /// </summary>
        /// <param name="user">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="user"/> or <paramref name="password"/> is null, empty, or whitespace.</exception>
        public FluentConnectionStringBuilder WithCredentials(string user, string password)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                throw new ArgumentException("User cannot be null or whitespace.", nameof(user));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
            }

            _user = user.Trim();
            _password = password;
            _integratedSecurity = false;
            return this;
        }

        /// <summary>
        /// Enables integrated (OS) security and clears any previously set credentials.
        /// </summary>
        /// <returns>The same builder instance for fluent chaining.</returns>
        public FluentConnectionStringBuilder WithIntegratedSecurity()
        {
            _integratedSecurity = true;
            _user = null;
            _password = null;
            return this;
        }

        /// <summary>
        /// Sets whether SSL/TLS is required for the connection. Defaults to required.
        /// </summary>
        /// <param name="required">True to require encryption; false to disable it.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        public FluentConnectionStringBuilder WithSsl(bool required = true)
        {
            _sslRequired = required;
            return this;
        }

        /// <summary>
        /// Sets the minimum and maximum connection pool sizes.
        /// </summary>
        /// <param name="min">The minimum pool size; must not be negative.</param>
        /// <param name="max">The maximum pool size; must not be less than <paramref name="min"/>.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public FluentConnectionStringBuilder WithPooling(int min, int max)
        {
            if (min < 0)
            {
                throw new ArgumentException("Minimum pool size cannot be negative.", nameof(min));
            }

            if (max < min)
            {
                throw new ArgumentException("Maximum pool size cannot be less than minimum pool size.", nameof(max));
            }

            _poolingMin = min;
            _poolingMax = max;
            return this;
        }

        /// <summary>
        /// Sets the connection timeout in seconds.
        /// </summary>
        /// <param name="seconds">The timeout in seconds; must be positive.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="seconds"/> is zero or negative.</exception>
        public FluentConnectionStringBuilder WithTimeout(int seconds)
        {
            if (seconds <= 0)
            {
                throw new ArgumentException("Timeout must be positive.", nameof(seconds));
            }

            _timeout = seconds;
            return this;
        }

        /// <summary>
        /// Adds or overwrites an arbitrary key/value option appended verbatim to the connection string.
        /// </summary>
        /// <param name="key">The option key.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is null, empty, or whitespace.</exception>
        public FluentConnectionStringBuilder WithOption(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }

            _options[key.Trim()] = value.Trim();
            return this;
        }

        /// <summary>
        /// Builds the connection string using the syntax of the configured provider.
        /// </summary>
        /// <returns>The composed connection string.</returns>
        /// <exception cref="InvalidOperationException">The provider is SQLite and no database path was configured.</exception>
        public string Build()
        {
            var builder = new StringBuilder();

            switch (_provider.ToLowerInvariant())
            {
                case "sqlserver":
                case "sqlclient":
                    BuildSqlServer(builder);
                    break;
                case "npgsql":
                case "postgresql":
                    BuildPostgreSql(builder);
                    break;
                case "mysql":
                case "mysql.data":
                    BuildMySql(builder);
                    break;
                case "sqlite":
                case "sqlitepclraw":
                    BuildSqlite(builder);
                    break;
                default:
                    BuildGeneric(builder);
                    break;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Quotes a value per ADO.NET connection string rules when it contains
        /// separators, quotes, or leading/trailing whitespace.
        /// </summary>
        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool needsQuotes = value.IndexOfAny(_charsRequiringQuotes) >= 0 ||
                               value[0] == ' ' ||
                               value[^1] == ' ';

            return needsQuotes
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        private void BuildSqlServer(StringBuilder builder)
        {
            var dataSource = new StringBuilder();
            if (!string.IsNullOrEmpty(_host))
            {
                dataSource.Append(Escape(_host));
                if (_port.HasValue)
                {
                    dataSource.Append($",{_port.Value}");
                }
            }

            builder.Append("Server=");
            builder.Append(dataSource);

            if (!string.IsNullOrEmpty(_database))
            {
                builder.Append(";Database=");
                builder.Append(Escape(_database));
            }

            if (_integratedSecurity)
            {
                builder.Append(";Integrated Security=True");
            }
            else if (!string.IsNullOrEmpty(_user))
            {
                builder.Append(";User Id=");
                builder.Append(Escape(_user));
                builder.Append(";Password=");
                builder.Append(Escape(_password));
            }

            if (_sslRequired)
            {
                builder.Append(";Encrypt=True;TrustServerCertificate=False");
            }
            else
            {
                builder.Append(";Encrypt=False");
            }

            if (_poolingMin.HasValue && _poolingMax.HasValue)
            {
                builder.Append(";Pooling=True;Min Pool Size=");
                builder.Append(_poolingMin.Value);
                builder.Append(";Max Pool Size=");
                builder.Append(_poolingMax.Value);
            }

            if (_timeout.HasValue)
            {
                builder.Append(";Connection Timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(Escape(option.Value));
            }
        }

        private void BuildPostgreSql(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_host))
            {
                builder.Append("Host=");
                builder.Append(Escape(_host));
                if (_port.HasValue)
                {
                    builder.Append(";Port=");
                    builder.Append(_port.Value);
                }
            }

            if (!string.IsNullOrEmpty(_database))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Database=");
                builder.Append(Escape(_database));
            }

            if (_integratedSecurity)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Integrated Security=true");
            }
            else if (!string.IsNullOrEmpty(_user))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Username=");
                builder.Append(Escape(_user));
                builder.Append(";Password=");
                builder.Append(Escape(_password));
            }

            if (builder.Length > 0) builder.Append(';');
            builder.Append("SSL Mode=");
            builder.Append(_sslRequired ? "Require" : "Disable");

            if (_poolingMin.HasValue && _poolingMax.HasValue)
            {
                builder.Append(";Minimum Pool Size=");
                builder.Append(_poolingMin.Value);
                builder.Append(";Maximum Pool Size=");
                builder.Append(_poolingMax.Value);
            }

            if (_timeout.HasValue)
            {
                builder.Append(";Timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(Escape(option.Value));
            }
        }

        private void BuildMySql(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_host))
            {
                builder.Append("Server=");
                builder.Append(Escape(_host));
                if (_port.HasValue)
                {
                    builder.Append(";Port=");
                    builder.Append(_port.Value);
                }
            }

            if (!string.IsNullOrEmpty(_database))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Database=");
                builder.Append(Escape(_database));
            }

            if (_integratedSecurity)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("IntegratedSecurity=true");
            }
            else if (!string.IsNullOrEmpty(_user))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Uid=");
                builder.Append(Escape(_user));
                builder.Append(";Pwd=");
                builder.Append(Escape(_password));
            }

            if (builder.Length > 0) builder.Append(';');
            builder.Append("SslMode=");
            builder.Append(_sslRequired ? "Required" : "None");

            if (_poolingMin.HasValue && _poolingMax.HasValue)
            {
                builder.Append(";Pooling=true;Minimum Pool Size=");
                builder.Append(_poolingMin.Value);
                builder.Append(";Maximum Pool Size=");
                builder.Append(_poolingMax.Value);
            }

            if (_timeout.HasValue)
            {
                builder.Append(";Connection Timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(Escape(option.Value));
            }
        }

        private void BuildSqlite(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_database))
            {
                builder.Append("Data Source=");
                builder.Append(Escape(_database));
            }

            if (_timeout.HasValue)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Default Timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(Escape(option.Value));
            }

            if (builder.Length == 0)
            {
                throw new InvalidOperationException("SQLite connection string requires a database path.");
            }
        }

        private void BuildGeneric(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_host))
            {
                builder.Append("host=");
                builder.Append(Escape(_host));
                if (_port.HasValue)
                {
                    builder.Append(";port=");
                    builder.Append(_port.Value);
                }
            }

            if (!string.IsNullOrEmpty(_database))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("database=");
                builder.Append(Escape(_database));
            }

            if (_integratedSecurity)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("integrated_security=true");
            }
            else if (!string.IsNullOrEmpty(_user))
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("user=");
                builder.Append(Escape(_user));
                builder.Append(";password=");
                builder.Append(Escape(_password));
            }

            if (builder.Length > 0) builder.Append(';');
            builder.Append("ssl=");
            builder.Append(_sslRequired ? "true" : "false");

            if (_poolingMin.HasValue && _poolingMax.HasValue)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("pooling_min=");
                builder.Append(_poolingMin.Value);
                builder.Append(";pooling_max=");
                builder.Append(_poolingMax.Value);
            }

            if (_timeout.HasValue)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(Escape(option.Value));
            }
        }

        /// <summary>
        /// Captures the current builder configuration as a serializable snapshot.
        /// </summary>
        internal FluentConnectionStringBuilderState CaptureState() => new()
        {
            Provider = _provider,
            Host = _host,
            Port = _port,
            Database = _database,
            User = _user,
            Password = _password,
            IntegratedSecurity = _integratedSecurity,
            SslRequired = _sslRequired,
            PoolingMin = _poolingMin,
            PoolingMax = _poolingMax,
            Timeout = _timeout,
            Options = new Dictionary<string, string>(_options),
        };

        /// <summary>
        /// Reconstructs a builder from a previously captured snapshot.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="ArgumentException">The snapshot has no provider.</exception>
        internal static FluentConnectionStringBuilder FromState(FluentConnectionStringBuilderState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (string.IsNullOrWhiteSpace(state.Provider))
            {
                throw new ArgumentException("State must contain a provider.", nameof(state));
            }

            var builder = new FluentConnectionStringBuilder(state.Provider)
            {
                _host = state.Host,
                _port = state.Port,
                _database = state.Database,
                _user = state.User,
                _password = state.Password,
                _integratedSecurity = state.IntegratedSecurity,
                _sslRequired = state.SslRequired,
            };

            builder._poolingMin = state.PoolingMin;
            builder._poolingMax = state.PoolingMax;
            builder._timeout = state.Timeout;

            if (state.Options is not null)
            {
                foreach (var option in state.Options)
                {
                    builder._options[option.Key] = option.Value;
                }
            }

            return builder;
        }
    }

    /// <summary>
    /// Serializable snapshot of a <see cref="FluentConnectionStringBuilder"/> configuration.
    /// </summary>
    internal sealed class FluentConnectionStringBuilderState
    {
        /// <summary>Gets or sets the provider name.</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Gets or sets the host.</summary>
        public string? Host { get; set; }

        /// <summary>Gets or sets the port.</summary>
        public int? Port { get; set; }

        /// <summary>Gets or sets the database name.</summary>
        public string? Database { get; set; }

        /// <summary>Gets or sets the user name.</summary>
        public string? User { get; set; }

        /// <summary>Gets or sets the password.</summary>
        public string? Password { get; set; }

        /// <summary>Gets or sets whether integrated security is enabled.</summary>
        public bool IntegratedSecurity { get; set; }

        /// <summary>Gets or sets whether SSL is required.</summary>
        public bool SslRequired { get; set; } = true;

        /// <summary>Gets or sets the minimum pool size.</summary>
        public int? PoolingMin { get; set; }

        /// <summary>Gets or sets the maximum pool size.</summary>
        public int? PoolingMax { get; set; }

        /// <summary>Gets or sets the connection timeout in seconds.</summary>
        public int? Timeout { get; set; }

        /// <summary>Gets or sets the additional options.</summary>
        public Dictionary<string, string>? Options { get; set; }
    }
}
