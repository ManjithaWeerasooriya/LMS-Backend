namespace LMS_Backend.Models.DTOs.Student;

public class StudentCompletionDto
{
    public int AttemptedQuizzes { get; set; }
    public int TotalQuizzes { get; set; }
    public double CompletionPercentage { get; set; }
}
