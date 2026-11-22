namespace Lexora.Architecture.Tests;

internal static class TestResultExtensions
{
    public static void ShouldBeSuccessful(this NetArchTest.Rules.TestResult result) =>
        result.IsSuccessful.ShouldBeTrue($"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
}
