using System.Security.Claims;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected IActionResult Success<T>(T data, string message) =>
        Ok(ApiResponse<T>.SuccessResponse(data, message));

    protected IActionResult SuccessMessage(string message) =>
        Ok(ApiResponse<object?>.SuccessResponse(null, message));

    protected IActionResult CreatedResponse<T>(
        string actionName,
        object routeValues,
        T data,
        string message) =>
        CreatedAtAction(actionName, routeValues, ApiResponse<T>.SuccessResponse(data, message));

    protected IActionResult UnauthorizedResponse() =>
        Unauthorized(ApiResponse<object?>.ErrorResponse("Authentication is required."));

    protected IActionResult HandleException(Exception exception) =>
        exception switch
        {
            NotFoundException => NotFound(ApiResponse<object?>.ErrorResponse(exception.Message)),
            ForbiddenException => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object?>.ErrorResponse(exception.Message)),
            ConflictException => Conflict(ApiResponse<object?>.ErrorResponse(exception.Message)),
            ServiceUnavailableException => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object?>.ErrorResponse(exception.Message)),
            InvalidOperationException => BadRequest(ApiResponse<object?>.ErrorResponse(exception.Message)),
            ArgumentException => BadRequest(ApiResponse<object?>.ErrorResponse(exception.Message)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object?>.ErrorResponse("An unexpected error occurred."))
        };
}
