using System;
using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Reports;

public class EnrollmentStatisticsDto
{
    public int TotalEnrollments { get; set; }
    public int TotalStudents { get; set; }
    public List<CourseEnrollmentStatDto> EnrollmentByCourse { get; set; } = new();
    public List<EnrollmentGrowthPointDto> MonthlyGrowth { get; set; } = new();
}

public class CourseEnrollmentStatDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public double AverageProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentGrowthPointDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Enrollments { get; set; }
}
