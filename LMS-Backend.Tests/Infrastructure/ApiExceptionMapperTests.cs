using LMS_Backend.Infrastructure.Exceptions;
using LMS_Backend.Models.Exceptions;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Tests.Infrastructure;

public class ApiExceptionMapperTests
{
    [Theory]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest, "bad request")]
    [InlineData(typeof(ArgumentNullException), StatusCodes.Status400BadRequest, "missing value")]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status403Forbidden, "forbidden")]
    [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound, "missing")]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, "not found")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status409Conflict, "conflict")]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict, "duplicate")]
    [InlineData(typeof(ServiceUnavailableException), StatusCodes.Status503ServiceUnavailable, "downstream unavailable")]
    [InlineData(typeof(Exception), StatusCodes.Status500InternalServerError, "An unexpected error occurred.")]
    public void Map_ReturnsExpectedStatusCode_AndMessage(
        Type exceptionType,
        int expectedStatusCode,
        string expectedMessage)
    {
        var exception = CreateException(exceptionType, expectedMessage);

        var result = ApiExceptionMapper.Map(exception);

        Assert.Equal(expectedStatusCode, result.StatusCode);
        Assert.False(result.Body.Success);
        if (exceptionType == typeof(ArgumentNullException))
        {
            Assert.StartsWith(expectedMessage, result.Body.Message, StringComparison.Ordinal);
            return;
        }

        Assert.Equal(expectedMessage, result.Body.Message);
    }

    private static Exception CreateException(Type exceptionType, string message)
    {
        return exceptionType == typeof(ArgumentNullException)
            ? new ArgumentNullException("value", message)
            : (Exception)Activator.CreateInstance(exceptionType, message)!;
    }
}
