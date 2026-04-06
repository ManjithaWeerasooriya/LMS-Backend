using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/quizzes/{quizId:guid}/attempts")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherQuizAttemptsController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public TeacherQuizAttemptsController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAttempts(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempts = await _quizService.GetQuizAttemptsAsync(teacherId, quizId, cancellationToken);
            return Success(attempts, "Quiz attempts retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptById(
        Guid quizId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempt = await _quizService.GetQuizAttemptByIdAsync(teacherId, quizId, attemptId, cancellationToken);
            return Success(attempt, "Quiz attempt retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{attemptId:guid}/answers/{answerId:guid}/grade")]
    public async Task<IActionResult> GradeAnswer(
        Guid quizId,
        Guid attemptId,
        Guid answerId,
        [FromBody] ManualGradeAnswerDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempt = await _quizService.GradeAnswerAsync(teacherId, quizId, attemptId, answerId, dto, cancellationToken);
            return Success(attempt, "Answer graded successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
