using System.Net.Http.Headers;
using System.Security.Cryptography;
using JasperFx.CommandLine;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Chunks;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Containers;
using Lexora.TestKit.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed class PipelineFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = PostgreSqlContainerFactory.Create();
    private readonly RabbitMqContainer _rabbitMq = RabbitMqContainerFactory.Create();
    private PipelineApiFactory? _api;
    private PipelineWorkerFactory? _worker;

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "lexora-pipeline-tests", Guid.CreateVersion7().ToString("N"));

    public FakeEmbeddingModel Embeddings { get; } = new(dimensions: 1024);

    private PipelineApiFactory Api => _api ?? throw new InvalidOperationException("The pipeline hosts have not started.");

    public async ValueTask InitializeAsync()
    {
        JasperFxEnvironment.AutoStartHost = true;
        var cancellationToken = TestContext.Current.CancellationToken;

        await Task.WhenAll(_database.StartAsync(cancellationToken), _rabbitMq.StartAsync(cancellationToken));

        await using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        var settings = new PipelineSettings(
            _database.GetConnectionString(),
            _rabbitMq.GetConnectionString(),
            StorageRoot,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

        _worker = new PipelineWorkerFactory(settings, Embeddings);
        _worker.StartServer();
        _api = new PipelineApiFactory(settings);
        _api.StartServer();
    }

    public async Task<(HttpClient Client, Guid KnowledgeBaseId)> MemberWithKnowledgeBaseAsync(CancellationToken cancellationToken)
    {
        string passwordHash;
        await using (var scope = Api.Services.CreateAsyncScope())
        {
            passwordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(TestUsers.Password);
        }

        var member = User.Create($"member-{Guid.CreateVersion7():N}@lexora.test", "Member", passwordHash, UserRole.Member, TimeProvider.System.GetUtcNow()).Value;
        var knowledgeBase = new KnowledgeBaseBuilder().WithName($"Pipeline {Guid.CreateVersion7():N}").CreatedBy(member.Id).Build();

        await using (var context = CreateDbContext())
        {
            context.Users.Add(member);
            context.KnowledgeBases.Add(knowledgeBase);
            await context.SaveChangesAsync(cancellationToken);
        }

        var client = Api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.Services.GetRequiredService<IAccessTokenIssuer>().Issue(member).Value);

        return (client, knowledgeBase.Id);
    }

    public async Task<List<DocumentChunk>> ChunksOfAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();

        return await context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ProcessingVersion)
            .ThenBy(chunk => chunk.Index)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_worker is not null)
        {
            await _worker.DisposeAsync();
        }

        await _rabbitMq.DisposeAsync();
        await _database.DisposeAsync();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }

    private LexoraDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<LexoraDbContext>();
        DbContextConfiguration.Configure(builder, _database.GetConnectionString());

        return new LexoraDbContext(builder.Options);
    }
}
