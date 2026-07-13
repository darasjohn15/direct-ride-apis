using DirectRide.Api.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace DirectRide.Api.Tests;

public class DatabaseConnectionStringTests
{
    [Fact]
    public void Get_WithDbEnvironmentSettings_BuildsConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5433;Database=local;Username=postgres;Password=password",
            ["DB_HOST"] = "direct-ride-db.example.us-east-1.rds.amazonaws.com",
            ["DB_PORT"] = "5432",
            ["DB_NAME"] = "directride",
            ["DB_USERNAME"] = "api_user",
            ["DB_PASSWORD"] = "secret",
            ["DB_SSL_MODE"] = "Require"
        });

        var connectionString = DatabaseConnectionString.Get(configuration);

        connectionString.Should().Contain("Host=direct-ride-db.example.us-east-1.rds.amazonaws.com");
        connectionString.Should().Contain("Port=5432");
        connectionString.Should().Contain("Database=directride");
        connectionString.Should().Contain("Username=api_user");
        connectionString.Should().Contain("Password=secret");
        connectionString.Should().Contain("SSL Mode=Require");
    }

    [Fact]
    public void Get_WithRdsEnvironmentSettings_BuildsConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RDS_HOSTNAME"] = "rds-host",
            ["RDS_PORT"] = "5432",
            ["RDS_DB_NAME"] = "directride",
            ["RDS_USERNAME"] = "rds_user",
            ["RDS_PASSWORD"] = "rds_password"
        });

        var connectionString = DatabaseConnectionString.Get(configuration);

        connectionString.Should().Contain("Host=rds-host");
        connectionString.Should().Contain("Database=directride");
        connectionString.Should().Contain("Username=rds_user");
        connectionString.Should().Contain("Password=rds_password");
    }

    [Fact]
    public void Get_WithoutIndividualSettings_UsesDefaultConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Port=5432;Database=directride;Username=postgres;Password=password"
        });

        var connectionString = DatabaseConnectionString.Get(configuration);

        connectionString.Should().Be("Host=db;Port=5432;Database=directride;Username=postgres;Password=password");
    }

    [Fact]
    public void Get_WithPartialIndividualSettings_ThrowsHelpfulError()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5433;Database=local;Username=postgres;Password=password",
            ["DB_HOST"] = "rds-host"
        });

        var act = () => DatabaseConnectionString.Get(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Missing: DB_NAME/DB_DATABASE/RDS_DB_NAME/PGDATABASE, DB_USERNAME/DB_USER/RDS_USERNAME/PGUSER, DB_PASSWORD/RDS_PASSWORD/PGPASSWORD*");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
