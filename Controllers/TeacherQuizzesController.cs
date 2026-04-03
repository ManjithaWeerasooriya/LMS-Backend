using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Models.DTOs.Teacher;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/quizzes")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherQuizzesController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public TeacherQuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> GetQuizzesByCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quizzes = await _quizService.GetTeacherQuizzesByCourseAsync(teacherId, courseId, cancellationToken);
            return Success(quizzes, "Quizzes retrieved successfully.");
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
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quiz = await _quizService.GetTeacherQuizByIdAsync(teacherId, quizId, cancellationToken);
            return Success(quiz, "Quiz retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{quizId:guid}/analytics")]
    public async Task<IActionResult> GetQuizAnalytics(
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
            var analytics = await _quizService.GetTeacherQuizAnalyticsAsync(teacherId, quizId, cancellationToken);
            return Success(analytics, "Quiz analytics retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(
        [FromBody] CreateQuizDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quiz = await _quizService.CreateQuizAsync(teacherId, dto, cancellationToken);
            return CreatedResponse(nameof(GetQuizById), new { quizId = quiz.Id }, quiz, "Quiz created successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{quizId:guid}")]
    public async Task<IActionResult> UpdateQuiz(
        Guid quizId,
        [FromBody] UpdateQuizDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quiz = await _quizService.UpdateQuizAsync(teacherId, quizId, dto, cancellationToken);
            return Success(quiz, "Quiz updated successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{quizId:guid}")]
    public async Task<IActionResult> DeleteQuiz(
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
            await _quizService.DeleteQuizAsync(teacherId, quizId, cancellationToken);
            return SuccessMessage("Quiz deleted successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{quizId:guid}/results/publish")]
    public async Task<IActionResult> PublishResults(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return await SetResultsPublication(quizId, true, cancellationToken);
    }

    [HttpPost("{quizId:guid}/results/unpublish")]
    public async Task<IActionResult> UnpublishResults(
        Guid quizId,
        CancellationToken cancellationToken)
    {
        return await SetResultsPublication(quizId, false, cancellationToken);
    }

    private async Task<IActionResult> SetResultsPublication(
        Guid quizId,
        bool publishResults,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var quiz = await _quizService.SetResultsPublicationAsync(teacherId, quizId, publishResults, cancellationToken);
            var message = publishResults
                ? "Quiz results published successfully."
                : "Quiz results unpublished successfully.";

            return Success(quiz, message);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
