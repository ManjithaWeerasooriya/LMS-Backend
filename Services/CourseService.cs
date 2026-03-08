using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class CourseService
{
    private readonly ApplicationDBContext _dbContext;

    public CourseService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Course> CreateCourseAsync(
        string teacherId,
        CreateCourseRequestDto dto,
        CancellationToken cancellationToken)
    {
        var course = new Course
        {
            TeacherId = teacherId,
            Title = dto.Title.Trim(),
            Category = dto.Category?.Trim(),
            Description = dto.Description?.Trim(),
            DurationHours = dto.DurationHours,
            Price = dto.Price,
            MaxStudents = dto.MaxStudents,
            DifficultyLevel = dto.DifficultyLevel?.Trim(),
            Prerequisites = dto.Prerequisites?.Trim(),
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return course;
    }

    public async Task<List<CourseListItemDto>> GetCoursesForTeacherAsync(
        string teacherId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Courses
            .Include(c => c.Enrollments)
            .Where(c => c.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.Title.Contains(search));
        }

        var courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                Category = c.Category,
                InstructorName = c.Teacher.FirstName + " " + c.Teacher.LastName,
                Students = c.Enrollments.Count,
                Price = c.Price,
                Rating = c.AverageRating,
                Status = c.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return courses;
    }

    public async Task<Course?> GetCourseAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Courses
            .Where(c => c.Id == id && c.TeacherId == teacherId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateCourseAsync(
        Guid id,
        string teacherId,
        CreateCourseRequestDto dto,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, teacherId, cancellationToken);
        if (course == null) return false;

        course.Title = dto.Title.Trim();
        course.Category = dto.Category?.Trim();
        course.Description = dto.Description?.Trim();
        course.DurationHours = dto.DurationHours;
        course.Price = dto.Price;
        course.MaxStudents = dto.MaxStudents;
        course.DifficultyLevel = dto.DifficultyLevel?.Trim();
        course.Prerequisites = dto.Prerequisites?.Trim();
        course.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, teacherId, cancellationToken);
        if (course == null) return false;

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

