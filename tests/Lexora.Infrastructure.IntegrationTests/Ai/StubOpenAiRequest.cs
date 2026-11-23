namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed record StubOpenAiRequest(string Path, string Authorization, string Body);
