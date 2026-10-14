using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Npgsql;

namespace Server.Tests;

/// <summary>
/// Boots the full Server host in-memory (including Minimal API validation)
/// against a dedicated <c>smart_pantry_tests</c> Postgres database, which is
/// created on demand. Override the connection with SMARTPANTRY_TEST_CONNECTION.
/// </summary>
public class PantryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string TestDatabase { get; } = $"smart_pantry_tests_{Guid.CreateVersion7():N}";

    public string ConnectionString =>
        Environment.GetEnvironmentVariable("SMARTPANTRY_TEST_CONNECTION") is { } configured
            ? new NpgsqlConnectionStringBuilder(configured) { Database = TestDatabase }.ConnectionString
            : $"Server=localhost;Database={TestDatabase};Uid=postgres;Pwd=mypassword123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString
            });
        });
    }

    public async Task InitializeAsync()
    {
        await RecreateDatabaseAsync();
        _ = Services; // Build the host: Program.cs runs migrations on the test database.
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            var maintenance = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            // No ClearAllPools: the pool is process-global and test classes run
            // in parallel, so clearing would abort siblings' in-flight commands.
            // DROP ... WITH (FORCE) already terminates backends on its own.
            await using var connection = new NpgsqlConnection(maintenance);
            await connection.OpenAsync();

            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best effort: leftover test databases are harmless.
        }

        Dispose();
    }

    private async Task RecreateDatabaseAsync()
    {
        var maintenance = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        // No ClearAllPools here either (see DisposeAsync): force-drop is enough.
        await using var connection = new NpgsqlConnection(maintenance);
        await connection.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();

        await using var create = new NpgsqlCommand(
            $"CREATE DATABASE \"{TestDatabase}\"", connection);
        await create.ExecuteNonQueryAsync();
    }
}