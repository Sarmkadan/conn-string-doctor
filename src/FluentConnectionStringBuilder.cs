using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace ConnStringDoctor
{
    /// <summary>
    /// Provides a fluent interface for constructing provider-specific connection strings.
    /// This class enables building connection strings for various database connection strings using method chaining,
    /// with proper escaping and provider-specific formatting.
    /// </summary>
    public sealed class FluentConnectionStringBuilder
    {
        private static readonly char[] _charsRequiringQuotes = { ';', '=', '"', '\'' };

        internal readonly string _provider;
        internal string? _host;
        internal int? _port;
        internal string? _database;
        internal string? _user;
        internal string? _password;
        internal bool _integratedSecurity;
        internal bool _sslRequired = true;
        internal int? _poolingMin;
        internal int? _poolingMax;
        internal int? _timeout;
        internal readonly Dictionary<string, string> _options = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentConnectionStringBuilder"/> class for the specified provider.
        /// </summary>
        /// <param name="provider">The provider name; unrecognized providers produce a generic key=value string.</param>
        /// <exception cref="ArgumentException"><paramref name="provider"/> is null, empty, or whitespace.</exception>
        private FluentConnectionStringBuilder(string provider)
        {
            _provider = provider.Trim();
        }

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

        /// <summary>
        /// Configures the host name and optional port for the database server.
        /// </summary>
        /// <param name="host">The server host name or IP address to connect to.</param>
        /// <param name="port">The optional TCP port number; if null, uses the provider's default port.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="host"/> is null, empty, or consists only of whitespace.</exception>
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
        /// Specifies the database name or file path for the connection.
        /// For SQL Server, PostgreSQL, and MySQL this is the database name.
        /// For SQLite this is the path to the SQLite database file.
        /// </summary>
        /// <param name="db">The database name or file path.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="db"/> is null, empty, or consists only of whitespace.</exception>
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
        /// Configures username and password authentication, automatically disabling integrated security.
        /// </summary>
        /// <param name="user">The user name for database authentication.</param>
        /// <param name="password">The password for the specified user.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="user"/> or <paramref name="password"/> is null, empty, or consists only of whitespace.</exception>
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
        /// Configures the connection to use Windows integrated security (also known as trusted connection).
        /// This clears any previously configured username and password credentials.
        /// </summary>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        public FluentConnectionStringBuilder WithIntegratedSecurity()
        {
            _integratedSecurity = true;
            _user = null;
            _password = null;
            return this;
        }

        /// <summary>
        /// Configures whether SSL/TLS encryption is required for the connection.
        /// Defaults to requiring encryption for security.
        /// </summary>
        /// <param name="required">True to require SSL/TLS encryption; false to allow unencrypted connections.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        public FluentConnectionStringBuilder WithSsl(bool required = true)
        {
            _sslRequired = required;
            return this;
        }

        /// <summary>
        /// Configures the connection pool minimum and maximum size limits.
        /// </summary>
        /// <param name="min">The minimum number of connections to maintain in the pool; must be non-negative.</param>
        /// <param name="max">The maximum number of connections allowed in the pool; must not be less than <paramref name="min"/>.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
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
        /// Configures the connection timeout duration in seconds.
        /// </summary>
        /// <param name="seconds">The connection timeout in seconds; must be a positive integer.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
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
        /// Adds or updates a custom connection string parameter that will be appended to the final connection string.
        /// </summary>
        /// <param name="key">The parameter name/key to add or update.</param>
        /// <param name="value">The parameter value to set.</param>
        /// <returns>The same builder instance, enabling method chaining.</returns>
        /// <exception cref="ArgumentException"><paramref name="key"/> or <paramref name="value"/> is null, empty, or consists only of whitespace.</exception>
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
        /// Constructs the final connection string using the syntax appropriate for the configured database provider.
        /// </summary>
        /// <returns>The complete connection string ready for use with the specified provider.</returns>
        /// <exception cref="InvalidOperationException">Thrown when building a SQLite connection string without a configured database path.</exception>
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
        /// Escapes a connection string value according to ADO.NET rules.
        /// Values containing separators (;, =), quotes (", '), or leading/trailing whitespace are wrapped in double quotes.
        /// Internal quotes are doubled to escape them.
        /// </summary>
        /// <param name="value">The value to escape; if null or empty, returns an empty string.</param>
        /// <returns>The escaped value ready for inclusion in a connection string.</returns>
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
        /// Creates a serializable snapshot of the current builder configuration.
        /// This allows the connection string configuration to be saved and later restored.
        /// </summary>
        /// <returns>A new <see cref="FluentConnectionStringBuilderState"/> instance containing the current configuration.</returns>
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
        /// Reconstructs a <see cref="FluentConnectionStringBuilder"/> instance from a previously captured configuration snapshot.
        /// </summary>
        /// <param name="state">The configuration snapshot to restore from.</param>
        /// <returns>A new builder instance configured with the values from the snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="ArgumentException">The snapshot does not contain a valid provider name.</exception>
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
    /// Represents a serializable snapshot of a <see cref="FluentConnectionStringBuilder"/> configuration.
    /// This allows connection string configurations to be saved and restored across application sessions.
    /// </summary>
    internal sealed class FluentConnectionStringBuilderState
    {
        /// <summary>
        /// Gets or sets the database provider name (e.g., "sqlserver", "postgresql", "mysql", "sqlite").
        /// This determines the connection string syntax used when building the final connection string.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the server host name or IP address to connect to.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the TCP port number to connect to on the database server.
        /// If null, the provider's default port will be used.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Gets or sets the database name or file path for the connection.
        /// For SQL Server, PostgreSQL, and MySQL this is the database name.
        /// For SQLite this is the path to the SQLite database file.
        /// </summary>
        public string? Database { get; set; }

        /// <summary>
        /// Gets or sets the user name for database authentication.
        /// </summary>
        public string? User { get; set; }

        /// <summary>
        /// Gets or sets the password for the specified user.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets whether integrated security (Windows authentication) is enabled.
        /// When true, username and password credentials are ignored.
        /// </summary>
        public bool IntegratedSecurity { get; set; }

        /// <summary>
        /// Gets or sets whether SSL/TLS encryption is required for the connection.
        /// Defaults to true for secure connections.
        /// </summary>
        public bool SslRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum number of connections to maintain in the connection pool.
        /// </summary>
        public int? PoolingMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool.
        /// </summary>
        public int? PoolingMax { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Gets or sets additional connection string parameters as key-value pairs.
        /// These parameters are appended verbatim to the connection string.
        /// </summary>
        public Dictionary<string, string>? Options { get; set; }
    }
}