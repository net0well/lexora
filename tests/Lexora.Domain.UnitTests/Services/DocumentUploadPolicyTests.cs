using Lexora.Domain.Documents;
using Lexora.Domain.Services;
using Lexora.Shared.Results;
using Lexora.TestKit.Builders;

namespace Lexora.Domain.UnitTests.Services;

public sealed class DocumentUploadPolicyTests
{
    private const long MaxFileSize = 50 * 1024 * 1024;
    private const long MaxTotalStorage = 10L * 1024 * 1024 * 1024;

    private static readonly UploadLimits _limits = UploadLimits.Create(MaxFileSize, MaxTotalStorage);

    [Fact]
    public void Evaluate_AcceptableUpload_Succeeds() =>
        Evaluate(sizeBytes: 1024).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Evaluate_ArchivedKnowledgeBase_FailsAsBusinessRule()
    {
        var result = DocumentUploadPolicy.Evaluate(new KnowledgeBaseBuilder().Archived().Build(), DocumentFormat.Pdf, 1024, _limits, 0);

        result.Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe("knowledgeBase.archived"),
            notification => notification.Type.ShouldBe(NotificationType.BusinessRule));
    }

    [Fact]
    public void Evaluate_UnknownFormat_FailsAsUnsupported() =>
        Evaluate(format: null).Notifications.Single().Key.ShouldBe("document.format.unsupported");

    [Fact]
    public void Evaluate_UndefinedFormatValue_FailsAsUnsupported() =>
        Evaluate(format: (DocumentFormat)99).Notifications.Single().Key.ShouldBe("document.format.unsupported");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Evaluate_EmptyFile_FailsOnlyAsEmpty(long sizeBytes) =>
        Evaluate(sizeBytes: sizeBytes, currentStorageBytes: MaxTotalStorage).Notifications.Single().Key.ShouldBe("document.size.empty");

    [Fact]
    public void Evaluate_FileAtSizeLimit_Succeeds() =>
        Evaluate(sizeBytes: MaxFileSize).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Evaluate_FileOverSizeLimit_FailsAsExceeded() =>
        Evaluate(sizeBytes: MaxFileSize + 1).Notifications.Single().Key.ShouldBe("document.size.exceeded");

    [Fact]
    public void Evaluate_UploadFillingStorageExactly_Succeeds() =>
        Evaluate(sizeBytes: 1024, currentStorageBytes: MaxTotalStorage - 1024).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Evaluate_UploadOverflowingStorage_FailsAsBusinessRule()
    {
        var result = Evaluate(sizeBytes: 1025, currentStorageBytes: MaxTotalStorage - 1024);

        result.Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe("storage.limit.exceeded"),
            notification => notification.Type.ShouldBe(NotificationType.BusinessRule));
    }

    [Fact]
    public void Evaluate_SeveralViolations_ReportsAllOfThem()
    {
        var result = DocumentUploadPolicy.Evaluate(
            new KnowledgeBaseBuilder().Archived().Build(),
            null,
            MaxFileSize + 1,
            _limits,
            MaxTotalStorage);

        result.Notifications.Select(notification => notification.Key).ShouldBe(
        [
            "knowledgeBase.archived",
            "document.format.unsupported",
            "document.size.exceeded",
            "storage.limit.exceeded",
        ]);
    }

    private static Result Evaluate(DocumentFormat? format = DocumentFormat.Pdf, long sizeBytes = 1024, long currentStorageBytes = 0) =>
        DocumentUploadPolicy.Evaluate(new KnowledgeBaseBuilder().Build(), format, sizeBytes, _limits, currentStorageBytes);
}
