namespace Lexora.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    public const string ConnectionStringName = "Lexora";

    public string WolverineSchema { get; init; } = string.Empty;
}
