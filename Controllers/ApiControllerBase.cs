using System.Security.Claims;
using LMS_Backend.Infrastructure.Exceptions;
using LMS_Backend.Models.DTOs.Common;
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

    protected IActionResult HandleException(Exception exception)
    {
        var error = ApiExceptionMapper.Map(exception);
        return error.StatusCode switch
        {
            StatusCodes.Status400BadRequest => BadRequest(error.Body),
            StatusCodes.Status404NotFound => NotFound(error.Body),
            StatusCodes.Status409Conflict => Conflict(error.Body),
            _ => StatusCode(error.StatusCode, error.Body)
        };
    }
}
