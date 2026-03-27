using DirectRide.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DirectRide.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("Testing"); // 👈 important

    builder.ConfigureServices(services =>
    {
        // Remove existing DbContext registrations
        services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
        services.RemoveAll(typeof(AppDbContext));

        // Create in-memory SQLite connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Register SQLite instead
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });

        // Build and initialize DB
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    });
}

    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose();
        base.Dispose(disposing);
    }
}