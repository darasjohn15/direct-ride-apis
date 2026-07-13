using Npgsql;

namespace DirectRide.Api.Data;

public static class DatabaseConnectionString
{
    private const string DefaultConnectionName = "DefaultConnection";

    private static readonly string[] HostKeys = ["DB_HOST", "RDS_HOSTNAME", "PGHOST", "Database:Host"];
    private static readonly string[] PortKeys = ["DB_PORT", "RDS_PORT", "PGPORT", "Database:Port"];
    private static readonly string[] DatabaseKeys = ["DB_NAME", "DB_DATABASE", "RDS_DB_NAME", "PGDATABASE", "Database:Name"];
    private static readonly string[] UsernameKeys = ["DB_USERNAME", "DB_USER", "RDS_USERNAME", "PGUSER", "Database:Username"];
    private static readonly string[] PasswordKeys = ["DB_PASSWORD", "RDS_PASSWORD", "PGPASSWORD", "Database:Password"];
    private static readonly string[] SslModeKeys = ["DB_SSL_MODE", "RDS_SSL_MODE", "PGSSLMODE", "Database:SslMode"];

    public static string Get(IConfiguration configuration)
    {
        var individualSettings = GetIndividualSettings(configuration);

        if (individualSettings.HasAnyValue)
        {
            return BuildFromIndividualSettings(individualSettings);
        }

        return configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DefaultConnectionName}' was not found.");
    }

    private static string BuildFromIndividualSettings(DatabaseConnectionSettings settings)
    {
        var missingKeys = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            missingKeys.Add("DB_HOST/RDS_HOSTNAME/PGHOST");
        }

        if (string.IsNullOrWhiteSpace(settings.Database))
        {
            missingKeys.Add("DB_NAME/DB_DATABASE/RDS_DB_NAME/PGDATABASE");
        }

        if (string.IsNullOrWhiteSpace(settings.Username))
        {
            missingKeys.Add("DB_USERNAME/DB_USER/RDS_USERNAME/PGUSER");
        }

        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            missingKeys.Add("DB_PASSWORD/RDS_PASSWORD/PGPASSWORD");
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Incomplete database configuration. Missing: {string.Join(", ", missingKeys)}.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = settings.Host,
            Port = settings.Port ?? 5432,
            Database = settings.Database,
            Username = settings.Username,
            Password = settings.Password
        };

        if (!string.IsNullOrWhiteSpace(settings.SslMode)
            && Enum.TryParse<SslMode>(settings.SslMode, ignoreCase: true, out var sslMode))
        {
            builder.SslMode = sslMode;
        }

        return builder.ConnectionString;
    }

    private static DatabaseConnectionSettings GetIndividualSettings(IConfiguration configuration)
    {
        return new DatabaseConnectionSettings(
            GetFirstValue(configuration, HostKeys),
            GetFirstIntValue(configuration, PortKeys),
            GetFirstValue(configuration, DatabaseKeys),
            GetFirstValue(configuration, UsernameKeys),
            GetFirstValue(configuration, PasswordKeys),
            GetFirstValue(configuration, SslModeKeys));
    }

    private static string? GetFirstValue(IConfiguration configuration, string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? GetFirstIntValue(IConfiguration configuration, string[] keys)
    {
        var value = GetFirstValue(configuration, keys);

        return int.TryParse(value, out var parsedValue) ? parsedValue : null;
    }

    private sealed record DatabaseConnectionSettings(
        string? Host,
        int? Port,
        string? Database,
        string? Username,
        string? Password,
        string? SslMode)
    {
        public bool HasAnyValue =>
            !string.IsNullOrWhiteSpace(Host)
            || Port.HasValue
            || !string.IsNullOrWhiteSpace(Database)
            || !string.IsNullOrWhiteSpace(Username)
            || !string.IsNullOrWhiteSpace(Password)
            || !string.IsNullOrWhiteSpace(SslMode);
    }
}
