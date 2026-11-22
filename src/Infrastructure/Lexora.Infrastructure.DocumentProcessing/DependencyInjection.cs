using Lexora.Application.Abstractions.Processing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.DocumentProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentProcessingAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

        return services;
    }
}
