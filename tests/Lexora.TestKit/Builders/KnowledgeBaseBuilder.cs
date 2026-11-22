using Lexora.Domain.KnowledgeBases;

namespace Lexora.TestKit.Builders;

public sealed class KnowledgeBaseBuilder
{
    private string _name = "Contracts";
    private string? _description;
    private ChunkingSettings _chunking = ChunkingSettings.Default;
    private Guid _createdBy = Guid.CreateVersion7();
    private DateTimeOffset _now = TestClock.Now;
    private bool _archived;

    public KnowledgeBaseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public KnowledgeBaseBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public KnowledgeBaseBuilder WithChunking(ChunkingSettings chunking)
    {
        _chunking = chunking;
        return this;
    }

    public KnowledgeBaseBuilder CreatedBy(Guid userId)
    {
        _createdBy = userId;
        return this;
    }

    public KnowledgeBaseBuilder CreatedAt(DateTimeOffset now)
    {
        _now = now;
        return this;
    }

    public KnowledgeBaseBuilder Archived()
    {
        _archived = true;
        return this;
    }

    public KnowledgeBase Build()
    {
        var knowledgeBase = KnowledgeBase.Create(_name, _description, _chunking, _createdBy, _now).Value;

        if (_archived)
        {
            knowledgeBase.Archive(_now);
        }

        knowledgeBase.ClearDomainEvents();

        return knowledgeBase;
    }
}
