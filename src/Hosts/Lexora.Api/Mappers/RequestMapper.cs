using Lexora.Api.Contracts.Requests;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Login;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Application.Retrieval.SearchKnowledgeBase;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.ResetUserPassword;

namespace Lexora.Api.Mappers;

public static class RequestMapper
{
    public static LoginCommand ToCommand(this LoginRequest request) => new(request.Email, request.Password);

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request) => new(request.CurrentPassword, request.NewPassword);

    public static CreateUserCommand ToCommand(this CreateUserRequest request) => new(request.Email, request.Name, request.Role, request.Password);

    public static ChangeUserRoleCommand ToCommand(this ChangeUserRoleRequest request, Guid userId) => new(userId, request.Role);

    public static ResetUserPasswordCommand ToCommand(this ResetUserPasswordRequest request, Guid userId) => new(userId, request.NewPassword);

    public static CreateKnowledgeBaseCommand ToCommand(this CreateKnowledgeBaseRequest request) =>
        new(request.Name, request.Description, request.ChunkMaxTokens, request.ChunkOverlapTokens);

    public static StartConversationCommand ToCommand(this StartConversationRequest request) => new(request.KnowledgeBaseId, request.Title);

    public static AskQuestionCommand ToCommand(this AskQuestionRequest request, Guid conversationId) =>
        new(conversationId, request.Question ?? string.Empty, string.IsNullOrWhiteSpace(request.Profile) ? null : request.Profile.Trim());

    public static SearchKnowledgeBaseQuery ToQuery(this SearchKnowledgeBaseRequest request, Guid knowledgeBaseId) =>
        new(knowledgeBaseId, request.Query ?? string.Empty, request.Mode, request.TopK);

    public static UpdateKnowledgeBaseCommand ToCommand(this UpdateKnowledgeBaseRequest request, Guid knowledgeBaseId) =>
        new(knowledgeBaseId, request.Name, request.Description, request.ChunkMaxTokens, request.ChunkOverlapTokens);
}
