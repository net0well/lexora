using Lexora.Application.Abstractions.Processing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.DocumentProcessing;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentProcessingAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DocumentProcessingOptions>()
            .Bind(configuration.GetSection(DocumentProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFormatTextExtractor, PlainTextExtractor>();
        services.AddSingleton<IFormatTextExtractor, MarkdownTextExtractor>();
        services.AddSingleton<IFormatTextExtractor, CsvTextExtractor>();
        services.AddSingleton<IFormatTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IFormatTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IFormatTextExtractor, XlsxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

        return services;
    }
}
