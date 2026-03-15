using System.Security.Claims;
using LMS_Backend.Models.DTOs.Quizzes;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/quizzes")]
[Authorize(Roles = "Teacher")]
public class TeacherQuizzesController : ControllerBase
{
    private readonly QuizService _quizService;

    public TeacherQuizzesController(QuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuizListItemDto>>> GetMyQuizzes(
        CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var quizzes = await _quizService.GetQuizzesForTeacherAsync(teacherId, cancellationToken);
        return Ok(quizzes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(
        [FromBody] CreateQuizRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var quiz = await _quizService.CreateQuizAsync(teacherId, dto, cancellationToken);
        if (quiz == null)
        {
            return BadRequest(new { message = "Invalid course for this teacher." });
        }

        return CreatedAtAction(nameof(GetMyQuizzes), new { id = quiz.Id }, new { quiz.Id });
    }
}

