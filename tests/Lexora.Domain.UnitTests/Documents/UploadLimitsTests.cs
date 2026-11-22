using Lexora.Domain.Documents;

namespace Lexora.Domain.UnitTests.Documents;

public sealed class UploadLimitsTests
{
    [Fact]
    public void Create_ValidLimits_KeepsThem()
    {
        var limits = UploadLimits.Create(50, 1000);

        limits.MaxFileSizeBytes.ShouldBe(50);
        limits.MaxTotalStorageBytes.ShouldBe(1000);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(50, 0)]
    [InlineData(-1, 1000)]
    public void Create_NonPositiveLimit_Throws(long maxFileSize, long maxTotalStorage) =>
        Should.Throw<ArgumentOutOfRangeException>(() => UploadLimits.Create(maxFileSize, maxTotalStorage));

    [Fact]
    public void Create_FileLimitAboveTotalLimit_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => UploadLimits.Create(2000, 1000));
}
