using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

#nullable enable

namespace ConnStringDoctor
{
    public class FluentConnectionStringBuilder
    {
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

        public FluentConnectionStringBuilder WithDatabase(string db)
        {
            if (string.IsNullOrWhiteSpace(db))
            {
                throw new ArgumentException("Database cannot be null or whitespace.", nameof(db));
            }

            _database = db.Trim();
            return this;
        }

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

        public FluentConnectionStringBuilder WithIntegratedSecurity()
        {
            _integratedSecurity = true;
            _user = null;
            _password = null;
            return this;
        }

        public FluentConnectionStringBuilder WithSsl(bool required = true)
        {
            _sslRequired = required;
            return this;
        }

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

        public FluentConnectionStringBuilder WithTimeout(int seconds)
        {
            if (seconds <= 0)
            {
                throw new ArgumentException("Timeout must be positive.", nameof(seconds));
            }

            _timeout = seconds;
            return this;
        }

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

        private void BuildSqlServer(StringBuilder builder)
        {
            var dataSource = new StringBuilder();
            if (!string.IsNullOrEmpty(_host))
            {
                dataSource.Append(_host);
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
                builder.Append(_database);
            }

            if (_integratedSecurity)
            {
                builder.Append(";Integrated Security=True");
            }
            else if (!string.IsNullOrEmpty(_user))
            {
                builder.Append(";User Id=");
                builder.Append(_user);
                builder.Append(";Password=");
                builder.Append(_password);
            }

            if (_sslRequired)
            {
                builder.Append(";Encrypt=True;TrustServerCertificate=");
                builder.Append(_sslRequired ? "False" : "True");
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
                builder.Append(option.Value);
            }
        }

        private void BuildPostgreSql(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_host))
            {
                builder.Append("Host=");
                builder.Append(_host);
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
                builder.Append(_database);
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
                builder.Append(_user);
                builder.Append(";Password=");
                builder.Append(_password);
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
                builder.Append(";Command Timeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(option.Value);
            }
        }

        private void BuildMySql(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_host))
            {
                builder.Append("Server=");
                builder.Append(_host);
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
                builder.Append(_database);
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
                builder.Append(_user);
                builder.Append(";Pwd=");
                builder.Append(_password);
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
                builder.Append(";ConnectionTimeout=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(option.Value);
            }
        }

        private void BuildSqlite(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_database))
            {
                builder.Append("Data Source=");
                builder.Append(_database);
            }

            if (_timeout.HasValue)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append("Pooling=");
                builder.Append(_timeout.Value);
            }

            foreach (var option in _options)
            {
                if (builder.Length > 0) builder.Append(';');
                builder.Append(option.Key);
                builder.Append('=');
                builder.Append(option.Value);
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
                builder.Append(_host);
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
                builder.Append(_database);
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
                builder.Append(_user);
                builder.Append(";password=");
                builder.Append(_password);
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
                builder.Append(option.Value);
            }
        }
    }
}