using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Courses;

public class CourseQueryOptions
{
    private const int MaxPageSize = 100;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? TeacherId { get; set; }
    public CourseStatus? Status { get; set; }
    public string? Search { get; set; }

    public void Normalize()
    {
        if (PageNumber <= 0)
        {
            PageNumber = 1;
        }

        if (PageSize <= 0)
        {
            PageSize = 20;
        }
        else if (PageSize > MaxPageSize)
        {
            PageSize = MaxPageSize;
        }

        TeacherId = string.IsNullOrWhiteSpace(TeacherId) ? null : TeacherId.Trim();
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }
}
