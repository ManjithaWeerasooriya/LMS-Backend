using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/student/dashboard")]
[Authorize(Policy = AppPolicies.StudentOnly)]
public class StudentDashboardController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public StudentDashboardController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("quiz-scores")]
    public async Task<IActionResult> GetQuizScores(CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var result = await _quizService.GetStudentQuizScoresByCourseAsync(studentId, cancellationToken);
            return Success(result, "Student quiz scores retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("average-score")]
    public async Task<IActionResult> GetAverageScore(CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var result = await _quizService.GetStudentAverageScoreAsync(studentId, cancellationToken);
            return Success(result, "Student average score retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("completion")]
    public async Task<IActionResult> GetCompletion(CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var result = await _quizService.GetStudentCompletionAsync(studentId, cancellationToken);
            return Success(result, "Student completion retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}