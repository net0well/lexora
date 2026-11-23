using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Registry;

public sealed record ChatProfileClient(string ProfileName, AiProfileSettings Profile, IChatClient Client);
