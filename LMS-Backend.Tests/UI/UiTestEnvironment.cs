using System;
using Xunit;

namespace LMS_Backend.Tests.UI;

internal static class UiTestEnvironment
{
    private const string RunUiTestsVariable = "RUN_UI_TESTS";
    private const string BaseUrlVariable = "LMS_UI_BASE_URL";

    public static string BaseUrl =>
        Environment.GetEnvironmentVariable(BaseUrlVariable)
        ?? throw new InvalidOperationException(
            $"Set {BaseUrlVariable} before running UI tests.");

    public static string? GetSkipReason()
    {
        var shouldRun = Environment.GetEnvironmentVariable(RunUiTestsVariable);
        if (!string.Equals(shouldRun, "true", StringComparison.OrdinalIgnoreCase))
        {
            return $"UI tests are disabled. Set {RunUiTestsVariable}=true to enable them.";
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BaseUrlVariable)))
        {
            return $"Set {BaseUrlVariable} to the running frontend URL before executing UI tests.";
        }

        return null;
    }
}

public sealed class UiTheoryAttribute : TheoryAttribute
{
    public UiTheoryAttribute()
    {
        Skip = UiTestEnvironment.GetSkipReason();
    }
}
