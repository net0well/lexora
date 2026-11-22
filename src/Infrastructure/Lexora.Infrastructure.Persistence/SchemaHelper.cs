namespace Lexora.Infrastructure.Persistence;

public static class SchemaHelper
{
    public const string Schema = "lexora";
    public const string MigrationsHistoryTable = "__ef_migrations_history";
    public const int EmbeddingDimensions = 1024;

    public const string Users = "users";
    public const string RefreshTokens = "refresh_tokens";
    public const string ApiKeys = "api_keys";
    public const string KnowledgeBases = "knowledge_bases";
    public const string Documents = "documents";
    public const string DocumentChunks = "document_chunks";
    public const string Conversations = "conversations";
    public const string ConversationMessages = "conversation_messages";
}
