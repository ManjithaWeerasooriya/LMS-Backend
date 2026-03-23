using System;
using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Reports;

public class QuizStatisticsDto
{
    public int TotalAttempts { get; set; }
    public double AverageScorePercent { get; set; }
    public List<QuizAverageScoreDto> AverageScorePerQuiz { get; set; } = new();
    public PerformanceBandDto PerformanceBands { get; set; } = new();
}

public class QuizAverageScoreDto
{
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public double AverageScorePercent { get; set; }
    public int Attempts { get; set; }
}

public class PerformanceBandDto
{
    public double ExcellentPercentage { get; set; }
    public double GoodPercentage { get; set; }
    public double AveragePercentage { get; set; }
    public double NeedsImprovementPercentage { get; set; }
}
