using Lexora.Application.Abstractions.Ai;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Api.IntegrationTests.Hosting;

public static class IndexedBaseSeeder
{
    public static async Task<Guid> SeedAsync(ApiFactory api, CancellationToken cancellationToken, params (string Content, int Page, string? Section)[] excerpts)
    {
        ArgumentNullException.ThrowIfNull(api);

        var admin = await api.SeedUserAsync($"admin-{Guid.CreateVersion7():N}@lexora.test", UserRole.Admin, TestUsers.Password);
        var knowledgeBase = new KnowledgeBaseBuilder().WithName($"Base {Guid.CreateVersion7():N}").CreatedBy(admin.Id).Build();
        var document = new DocumentBuilder()
            .InKnowledgeBase(knowledgeBase.Id)
            .UploadedBy(admin.Id)
            .WithFileName("manual.pdf", DocumentFormat.Pdf)
            .InStatus(DocumentStatus.Indexed)
            .Build();
        var embeddings = api.Services.GetRequiredService<IEmbeddingModel>();
        var vectors = await embeddings.EmbedAsync([.. excerpts.Select(excerpt => excerpt.Content)], EmbeddingPurpose.Document, cancellationToken);

        await using var context = api.CreateDbContext();
        context.KnowledgeBases.Add(knowledgeBase);
        context.Documents.Add(document);

        for (var index = 0; index < excerpts.Length; index++)
        {
            var (content, page, section) = excerpts[index];
            var chunk = DocumentChunk.Create(knowledgeBase.Id, document.Id, 1, index, content, 10, ChunkLocation.Create(page, null, section, 0, content.Length));
            chunk.AttachEmbedding(vectors[index], embeddings.Space.Key, TestClock.Now);
            context.DocumentChunks.Add(chunk);
        }

        await context.SaveChangesAsync(cancellationToken);

        return knowledgeBase.Id;
    }
}
