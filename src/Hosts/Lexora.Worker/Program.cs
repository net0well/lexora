using JasperFx;
using Lexora.Application;
using Lexora.Domain;
using Lexora.Hosting;
using Lexora.Hosting.Health;
using Lexora.Infrastructure.AI;
using Lexora.Infrastructure.Caching;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.Infrastructure.Identity;
using Lexora.Infrastructure.Messaging;
using Lexora.Infrastructure.Persistence;
using Lexora.Infrastructure.Storage;
using Lexora.Shared;
using Lexora.Worker;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHostingDefaults(builder.Configuration, builder.Environment)
    .AddShared()
    .AddDomain()
    .AddApplication(builder.Configuration)
    .AddStorageAdapter(builder.Configuration)
    .AddAiAdapter(builder.Configuration)
    .AddDocumentProcessingAdapter(builder.Configuration)
    .AddCachingAdapter(builder.Configuration)
    .AddIdentityAdapter(builder.Configuration)
    .AddWorker(builder.Configuration);

builder.Host.UseWolverine(options => options
    .AddMessagingAdapter(builder.Configuration, MessagingHostRole.Worker)
    .AddPersistenceAdapter(builder.Configuration));

var app = builder.Build();

app.MapHealthEndpoints();

return await app.RunJasperFxCommands(args);
