using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OnlineLearningApp.Data;

public sealed class OnlineLearningAppDbContextFactory : IDesignTimeDbContextFactory<OnlineLearningAppDbContext>
{
    public OnlineLearningAppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is required for EF Core design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<OnlineLearningAppDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 23)));

        return new OnlineLearningAppDbContext(optionsBuilder.Options);
    }
}
