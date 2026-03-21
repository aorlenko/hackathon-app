using System.Text.Json;
using MarketService.Application.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace MarketService.Infrastructure.Persistence;

public sealed class MarketDbContextFactory : IDesignTimeDbContextFactory<MarketDbContext>
{
    public MarketDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarketDbContext>();
        optionsBuilder.UseSqlServer(ResolveConnectionString());
        return new MarketDbContext(optionsBuilder.Options, Options.Create(new TradingPetsOptions()));
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
            "MarketService"));

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
            $"Could not resolve ConnectionStrings:Sql for MarketService. Checked appsettings files under '{appProjectPath}' and the ConnectionStrings__Sql environment variable.");
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
