namespace LMS_Backend.Models.DTOs.Student;

public class StudentCourseQuizResultsDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int AttemptedQuizzes { get; set; }
    public int TotalQuizzes { get; set; }
    public double ProgressPercentage { get; set; }
    public decimal CourseAverageScore { get; set; }
    public List<StudentQuizScoreItemDto> Quizzes { get; set; } = new();
}
