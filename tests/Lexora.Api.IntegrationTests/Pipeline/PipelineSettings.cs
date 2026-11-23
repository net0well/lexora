namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed record PipelineSettings(string DatabaseConnectionString, string RabbitMqConnectionString, string StorageRoot, string SigningKey);
