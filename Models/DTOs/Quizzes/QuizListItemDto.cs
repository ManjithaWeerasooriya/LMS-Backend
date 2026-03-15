using System;

namespace LMS_Backend.Models.DTOs.Quizzes;

public class QuizListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int DurationMinutes { get; set; }
    public int Attempts { get; set; }
    public double AverageScorePercent { get; set; }
}

