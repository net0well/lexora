using FluentValidation;

namespace Lexora.Application.Documents.UploadDocument;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().WithErrorCode("document.fileName.invalid");
        RuleFor(command => command.Length).GreaterThan(0).WithErrorCode("document.size.empty");
        RuleFor(command => command.Content).NotNull().WithErrorCode("document.size.empty");
    }
}
