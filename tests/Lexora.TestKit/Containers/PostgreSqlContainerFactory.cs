using Testcontainers.PostgreSql;

namespace Lexora.TestKit.Containers;

public static class PostgreSqlContainerFactory
{
    public const string Image = "pgvector/pgvector:pg18";
    public const string Database = "lexora";
    public const string Username = "lexora";

    public static PostgreSqlContainer Create() =>
        new PostgreSqlBuilder(Image)
            .WithDatabase(Database)
            .WithUsername(Username)
            .WithPassword("lexora-tests")
            .Build();
}
