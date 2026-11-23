using System.ComponentModel.DataAnnotations;
using Lexora.Shared.Configuration;

namespace Lexora.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string RabbitMqConnectionStringName = ConnectionStringNames.RabbitMq;

    [Range(1, 64)]
    public int IngestionParallelism { get; init; } = 4;

    [Range(1, 64)]
    public int EmbeddingParallelism { get; init; } = 1;

    [Range(1, 1_000)]
    public ushort PreFetchCount { get; init; } = 10;
}
