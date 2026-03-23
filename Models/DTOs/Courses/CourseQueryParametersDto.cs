using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Courses;

public class CourseQueryParametersDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? TeacherId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }

    public CourseQueryOptions ToOptions()
    {
        CourseStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(Status) &&
            Enum.TryParse(Status, true, out CourseStatus statusValue))
        {
            parsedStatus = statusValue;
        }

        return new CourseQueryOptions
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            TeacherId = TeacherId,
            Status = parsedStatus,
            Search = Search
        };
    }
}
