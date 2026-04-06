using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Common;
using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/quizzes/{quizId:guid}/questions")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class TeacherQuizQuestionsController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public TeacherQuizQuestionsController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(
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
            var questions = await _quizService.GetQuestionsAsync(teacherId, quizId, cancellationToken);
            return Success(questions, "Questions retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{questionId:guid}")]
    public async Task<IActionResult> GetQuestionById(
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var question = await _quizService.GetQuestionByIdAsync(teacherId, quizId, questionId, cancellationToken);
            return Success(question, "Question retrieved successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuestion(
        Guid quizId,
        [FromBody] CreateQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var question = await _quizService.CreateQuestionAsync(teacherId, quizId, dto, cancellationToken);
            return CreatedResponse(nameof(GetQuestionById), new { quizId, questionId = question.Id }, question, "Question created successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid quizId,
        Guid questionId,
        [FromBody] UpdateQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            var question = await _quizService.UpdateQuestionAsync(teacherId, quizId, questionId, dto, cancellationToken);
            return Success(question, "Question updated successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return UnauthorizedResponse();
        }

        try
        {
            await _quizService.DeleteQuestionAsync(teacherId, quizId, questionId, cancellationToken);
            return SuccessMessage("Question deleted successfully.");
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
