using LMS_Backend.Models.DTOs.Quiz;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _quizService.CreateQuizAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(Guid courseId)
    {
        var quizzes = await _quizService.GetQuizzesByCourseAsync(courseId);
        return Ok(quizzes);
    }

    [HttpGet("{quizId}")]
    public async Task<IActionResult> GetById(Guid quizId)
    {
        var quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (quiz == null)
            return NotFound(new { message = "Quiz not found." });

        return Ok(quiz);
    }

    [HttpPut("{quizId}")]
    public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromBody] UpdateQuizDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _quizService.UpdateQuizAsync(quizId, dto);
            if (updated == null)
                return NotFound(new { message = "Quiz not found." });

            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{quizId}")]
    public async Task<IActionResult> DeleteQuiz(Guid quizId)
    {
        var deleted = await _quizService.DeleteQuizAsync(quizId);
        if (!deleted)
            return NotFound(new { message = "Quiz not found." });

        return Ok(new { message = "Quiz deleted successfully." });
    }
}