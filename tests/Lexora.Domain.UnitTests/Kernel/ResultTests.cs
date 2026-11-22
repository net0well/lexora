using Lexora.Shared.Results;

namespace Lexora.Domain.UnitTests.Kernel;

public sealed class ResultTests
{
    private static readonly Notification _notFound = Notification.NotFound("user.notFound", "User not found.");
    private static readonly Notification _conflict = Notification.Conflict("user.email.duplicate", "E-mail already in use.");

    [Fact]
    public void Success_WithoutValue_HasNoNotifications()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_WithNotifications_KeepsThemInOrder()
    {
        var result = Result.Failure(_notFound, _conflict);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Notifications.ShouldBe([_notFound, _conflict]);
    }

    [Fact]
    public void Failure_FromSequence_CopiesNotifications()
    {
        List<Notification> source = [_notFound];

        var result = Result.Failure(source);
        source.Add(_conflict);

        result.Notifications.ShouldBe([_notFound]);
    }

    [Fact]
    public void Failure_WithoutNotifications_Throws() => Should.Throw<ArgumentException>(() => Result.Failure());

    [Fact]
    public void FailureOfT_WithoutNotifications_Throws() => Should.Throw<ArgumentException>(() => Result.Failure<int>([]));

    [Fact]
    public void SuccessOfT_WithValue_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void SuccessOfT_WithNullableValue_ExposesNull()
    {
        var result = Result.Success<string?>(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void FailureOfT_ReadingValue_Throws()
    {
        var result = Result.Failure<int>(_notFound);

        result.IsFailure.ShouldBeTrue();
        result.Notifications.ShouldBe([_notFound]);
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "lexora";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("lexora");
    }

    [Fact]
    public void ResultOfT_UsedAsResult_KeepsFailureState()
    {
        Result result = Result.Failure<int>(_conflict);

        result.IsFailure.ShouldBeTrue();
        result.Notifications.ShouldBe([_conflict]);
    }
}
