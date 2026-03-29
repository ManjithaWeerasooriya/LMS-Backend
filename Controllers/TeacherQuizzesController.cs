using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/teacher/quizzes")]
[Authorize(Roles = "Teacher")]
public class TeacherQuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public TeacherQuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<QuizResponseDto>>> GetQuizzesByCourse(
        Guid courseId)
    {
        var quizzes = await _quizService.GetQuizzesByCourseAsync(courseId);
        return Ok(quizzes);
    }

    [HttpGet("{quizId:guid}")]
    public async Task<ActionResult<QuizResponseDto>> GetQuizById(Guid quizId)
    {
        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null)
        {
            return NotFound(new { message = "Quiz not found." });
        }

        return Ok(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var quiz = await _quizService.CreateQuizAsync(dto);
            return CreatedAtAction(nameof(GetQuizById), new { quizId = quiz.Id }, quiz);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{quizId:guid}")]
    public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] UpdateQuizDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var quiz = await _quizService.UpdateQuizAsync(quizId, dto);
            if (quiz == null)
            {
                return NotFound(new { message = "Quiz not found." });
            }

            return Ok(quiz);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{quizId:guid}")]
    public async Task<IActionResult> DeleteQuiz(Guid quizId)
    {
        var deleted = await _quizService.DeleteQuizAsync(quizId);
        if (!deleted)
        {
            return NotFound(new { message = "Quiz not found." });
        }

        return Ok(new { message = "Quiz deleted successfully." });
    }
}