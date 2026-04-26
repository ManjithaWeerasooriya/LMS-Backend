using LMS_Backend.Infrastructure.Auth;

namespace LMS_Backend.Tests.ParameterizedXUnitTests;

// Parameterized tests for role normalization logic used during user registration
public class AppRolesParameterizedTests
{
    // Verifies different role inputs (valid, invalid, whitespace, null) using [Theory]
    [Theory]
    [InlineData("Teacher", true, AppRoles.Teacher)]
    [InlineData(" teacher ", true, AppRoles.Teacher)]
    [InlineData("STUDENT", true, AppRoles.Student)]
    [InlineData("Admin", false, "")]
    [InlineData("   ", false, "")]
    [InlineData(null, false, "")]
    public void TryNormalizeRequestedRole_ShouldReturnExpectedResult(
        string? input,
        bool expectedResult,
        string expectedNormalizedRole)
    {
        // Arrange

        // Act
        var result = AppRoles.TryNormalizeRequestedRole(input, out var normalizedRole);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedNormalizedRole, normalizedRole);
    }
}
