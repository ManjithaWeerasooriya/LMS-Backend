using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student/quizzes")]
[Authorize(Policy = AppPolicies.StudentOnly)]
public class StudentQuizzesController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public StudentQuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyQuizzes(
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quizzes = await _quizService.GetStudentQuizzesAsync(studentId, cancellationToken);
            return Success(quizzes, "Student quizzes retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{quizId:guid}")]
    public async Task<IActionResult> GetQuizById(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quiz = await _quizService.GetStudentQuizByIdAsync(studentId, quizId, cancellationToken);
            return Success(quiz, "Quiz retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{quizId:guid}/result")]
    public async Task<IActionResult> GetQuizResult(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var result = await _quizService.GetStudentQuizResultAsync(studentId, quizId, cancellationToken);
            var message = result.AreResultsPublished
                ? "Quiz result retrieved successfully."
                : "Quiz results are not yet released.";

            return Success(result, message);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{quizId:guid}/attempts")]
    public async Task<IActionResult> StartAttempt(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempt = await _quizService.StartQuizAttemptAsync(studentId, quizId, cancellationToken);
            return CreatedResponse(nameof(GetAttemptById), new { attemptId = attempt.AttemptId }, attempt, "Quiz attempt started successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptById(
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempt = await _quizService.GetStudentAttemptByIdAsync(studentId, attemptId, cancellationToken);
            return Success(attempt, "Quiz attempt retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> SubmitAttempt(
        Guid attemptId,
        [FromBody] SubmitQuizAttemptDto dto,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var attempt = await _quizService.SubmitQuizAttemptAsync(studentId, attemptId, dto, cancellationToken);
            return Success(attempt, "Quiz attempt submitted successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
