using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.Exceptions;
using Microsoft.AspNetCore.Http;

namespace LMS_Backend.Infrastructure.Exceptions;

public static class ApiExceptionMapper
{
    public static ApiErrorResult Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            ArgumentNullException => Create(StatusCodes.Status400BadRequest, exception.Message),
            ArgumentException => Create(StatusCodes.Status400BadRequest, exception.Message),
            UnauthorizedAccessException => Create(StatusCodes.Status403Forbidden, exception.Message),
            ForbiddenException => Create(StatusCodes.Status403Forbidden, exception.Message),
            KeyNotFoundException => Create(StatusCodes.Status404NotFound, exception.Message),
            NotFoundException => Create(StatusCodes.Status404NotFound, exception.Message),
            ConflictException => Create(StatusCodes.Status409Conflict, exception.Message),
            InvalidOperationException => Create(StatusCodes.Status409Conflict, exception.Message),
            ServiceUnavailableException => Create(StatusCodes.Status503ServiceUnavailable, exception.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
    }

    private static ApiErrorResult Create(int statusCode, string message)
    {
        return new ApiErrorResult(statusCode, ApiResponse<object?>.ErrorResponse(message));
    }
}

public sealed record ApiErrorResult(
    int StatusCode,
    ApiResponse<object?> Body);
