using Lexora.Infrastructure.Storage.S3;

namespace Lexora.Infrastructure.IntegrationTests.Storage;

public sealed class AttachmentDispositionTests
{
    [Fact]
    public void For_AsciiName_IsAnAttachmentWithBothFileNameForms() =>
        AttachmentDisposition.For("manual.pdf").ShouldBe("attachment; filename=\"manual.pdf\"; filename*=utf-8''manual.pdf");

    [Fact]
    public void For_NonAsciiName_EncodesItAndKeepsAnAsciiFallback()
    {
        var disposition = AttachmentDisposition.For("relatório \"final\".pdf");

        disposition.ShouldStartWith("attachment; filename=\"relat_rio _final_.pdf\"");
        disposition.ShouldContain("filename*=utf-8''relat%C3%B3rio%20%22final%22.pdf");
    }
}
