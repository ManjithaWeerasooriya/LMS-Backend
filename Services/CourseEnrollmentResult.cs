namespace LMS_Backend.Services;

public class CourseEnrollmentResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
