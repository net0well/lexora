using System.Security.Cryptography;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.CommandLine;
using JasperFx.Resources;
using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence;
using Lexora.Shared.Configuration;
using Lexora.TestKit.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;

namespace Lexora.Api.IntegrationTests.Hosting;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EnvironmentName = "Testing";

    private static readonly string _truncateDomainTables = "TRUNCATE " + string.Join(
        ", ",
        new[]
        {
            SchemaHelper.Users,
            SchemaHelper.RefreshTokens,
            SchemaHelper.ApiKeys,
            SchemaHelper.KnowledgeBases,
            SchemaHelper.Documents,
            SchemaHelper.DocumentChunks,
            SchemaHelper.Conversations,
            SchemaHelper.ConversationMessages,
        }.Select(table => $"{SchemaHelper.Schema}.{table}")) + " CASCADE";

    private readonly PostgreSqlContainer _database = PostgreSqlContainerFactory.Create();
    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public async ValueTask InitializeAsync()
    {
        JasperFxEnvironment.AutoStartHost = true;

        await _database.StartAsync(TestContext.Current.CancellationToken);

        await using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        StartServer();
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async Task<User> SeedUserAsync(string email, UserRole role, string password, bool active = true)
    {
        await using var scope = Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        var user = User.Create(email, email.Split('@')[0], hasher.Hash(password), role, now).Value;

        if (!active)
        {
            user.Deactivate(now);
        }

        await using var context = CreateDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    public LexoraDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<LexoraDbContext>();
        DbContextConfiguration.Configure(builder, _database.GetConnectionString());

        return new LexoraDbContext(builder.Options);
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(_truncateDomainTables, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.Database}", _database.GetConnectionString());
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.RabbitMq}", "amqp://guest:guest@localhost:5672");
        builder.UseSetting("Jwt:SigningKey", _signingKey);
        builder.UseSetting("RateLimiting:Auth:LoginPermitsPerMinute", "10000");
        builder.UseSetting("RateLimiting:Auth:RefreshPermitsPerMinute", "10000");

        builder.ConfigureServices(services =>
        {
            services.CritterStackDefaults(defaults =>
            {
                defaults.Production.GeneratedCodeMode = TypeLoadMode.Dynamic;
                defaults.Production.AssertAllPreGeneratedTypesExist = false;
                defaults.Production.ResourceAutoCreate = AutoCreate.CreateOrUpdate;
            });

            services.DisableAllExternalWolverineTransports();
            services.AddResourceSetupOnStartup();
        });
    }
}
