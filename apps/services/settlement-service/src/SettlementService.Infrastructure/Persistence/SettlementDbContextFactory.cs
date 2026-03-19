using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SettlementService.Infrastructure.Persistence;

public sealed class SettlementDbContextFactory : IDesignTimeDbContextFactory<SettlementDbContext>
{
    public SettlementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SettlementDbContext>();
        optionsBuilder.UseSqlServer(ResolveConnectionString());
        return new SettlementDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Sql");
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var appProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SettlementService"));

        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            "Development";

        var connectionString = ReadConnectionString(Path.Combine(appProjectPath, "appsettings.json"));
        var environmentConnection = ReadConnectionString(Path.Combine(appProjectPath, $"appsettings.{environmentName}.json"));
        if (!string.IsNullOrWhiteSpace(environmentConnection))
        {
            connectionString = environmentConnection;
        }

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Could not resolve ConnectionStrings:Sql for SettlementService. Checked appsettings files under '{appProjectPath}' and the ConnectionStrings__Sql environment variable.");
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        if (!connectionStrings.TryGetProperty("Sql", out var sqlConnection))
        {
            return null;
        }

        return sqlConnection.GetString();
    }
}
