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
        var status = CourseStatus.Active;
        if (!string.IsNullOrWhiteSpace(dto.Status) &&
            Enum.TryParse<CourseStatus>(dto.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

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
            Status = status,
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

    public async Task<CourseDetailDto?> GetCourseDetailForTeacherAsync(
        Guid id,
        string teacherId,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Where(c => c.Id == id && c.TeacherId == teacherId)
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null) return null;

        return new CourseDetailDto
        {
            Id = course.Id,
            Title = course.Title,
            Category = course.Category,
            Description = course.Description,
            DurationHours = course.DurationHours,
            Price = course.Price,
            MaxStudents = course.MaxStudents,
            DifficultyLevel = course.DifficultyLevel,
            Prerequisites = course.Prerequisites,
            Status = course.Status.ToString()
        };
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

        if (!string.IsNullOrWhiteSpace(dto.Status) &&
            Enum.TryParse<CourseStatus>(dto.Status, true, out var parsedStatus))
        {
            course.Status = parsedStatus;
        }

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

    public class CourseEnrollmentResult
    {
        public bool Success { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public async Task<CourseEnrollmentResult> EnrollStudentInCourseAsync(
        Guid courseId,
        string studentId,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
        {
            return new CourseEnrollmentResult
            {
                Success = false,
                ErrorCode = "CourseNotFound",
                ErrorMessage = "Course not found."
            };
        }

        if (course.Status != CourseStatus.Active)
        {
            return new CourseEnrollmentResult
            {
                Success = false,
                ErrorCode = "CourseNotActive",
                ErrorMessage = "Course is not open for enrollment."
            };
        }

        var existing = course.Enrollments.FirstOrDefault(e => e.StudentId == studentId);
        if (existing is not null)
        {
            // Idempotent: already enrolled is treated as success.
            return new CourseEnrollmentResult { Success = true };
        }

        if (course.MaxStudents > 0 && course.Enrollments.Count >= course.MaxStudents)
        {
            return new CourseEnrollmentResult
            {
                Success = false,
                ErrorCode = "CourseFull",
                ErrorMessage = "Course has reached its maximum capacity."
            };
        }

        var enrollment = new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = studentId,
            EnrolledAt = DateTime.UtcNow,
            ProgressPercent = 0
        };

        _dbContext.CourseEnrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CourseEnrollmentResult { Success = true };
    }

    public async Task<List<StudentCourseListItemDto>> GetCoursesForStudentAsync(
        string studentId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Teacher)
            .Where(c => c.Status == CourseStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.Title.Contains(search));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new StudentCourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                Category = c.Category,
                InstructorName = c.Teacher.FirstName + " " + c.Teacher.LastName,
                StudentsEnrolled = c.Enrollments.Count,
                Price = c.Price,
                Rating = c.AverageRating,
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == studentId)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StudentCourseListItemDto>> GetEnrolledCoursesForStudentAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Teacher)
            .Where(c => c.Status == CourseStatus.Active &&
                        c.Enrollments.Any(e => e.StudentId == studentId));

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new StudentCourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                Category = c.Category,
                InstructorName = c.Teacher.FirstName + " " + c.Teacher.LastName,
                StudentsEnrolled = c.Enrollments.Count,
                Price = c.Price,
                Rating = c.AverageRating,
                IsEnrolled = true
            })
            .ToListAsync(cancellationToken);
    }
}


