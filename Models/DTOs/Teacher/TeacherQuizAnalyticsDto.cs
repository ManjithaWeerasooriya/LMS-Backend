namespace LMS_Backend.Models.DTOs.Teacher;

public class TeacherQuizAnalyticsDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal TotalMarks { get; set; }
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
    public decimal LowestScore { get; set; }
    public double PassPercentage { get; set; }
    public double FailPercentage { get; set; }
    public double ParticipationRate { get; set; }
    public int TotalEnrolledStudents { get; set; }
    public int StudentsParticipated { get; set; }
}
