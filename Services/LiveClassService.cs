using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.LiveClasses;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class LiveClassService
{
    private readonly ApplicationDBContext _dbContext;

    public LiveClassService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LiveClass?> ScheduleLiveClassAsync(
        string teacherId,
        ScheduleLiveClassRequestDto dto,
        CancellationToken cancellationToken)
    {
        Course? course = null;
        if (dto.CourseId.HasValue)
        {
            course = await _dbContext.Courses
                .FirstOrDefaultAsync(
                    c => c.Id == dto.CourseId.Value && c.TeacherId == teacherId,
                    cancellationToken);

            if (course == null)
            {
                return null;
            }
        }

        var liveClass = new LiveClass
        {
            TeacherId = teacherId,
            CourseId = course?.Id,
            Topic = dto.Topic.Trim(),
            ScheduledAt = dto.ScheduledAt,
            MeetingLink = dto.MeetingLink?.Trim(),
            EnableRecording = dto.EnableRecording,
            DurationMinutes = dto.DurationMinutes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.LiveClasses.Add(liveClass);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return liveClass;
    }

    public async Task<List<LiveClassListItemDto>> GetUpcomingForTeacherAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var liveClasses = await _dbContext.LiveClasses
            .Include(l => l.Course)
            .Where(l => l.TeacherId == teacherId && l.ScheduledAt >= nowUtc)
            .OrderBy(l => l.ScheduledAt)
            .Select(l => new LiveClassListItemDto
            {
                Id = l.Id,
                Topic = l.Topic,
                CourseTitle = l.Course != null ? l.Course.Title : null,
                ScheduledAt = l.ScheduledAt,
                StudentsEnrolled = l.Course != null
                    ? l.Course.Enrollments.Count
                    : 0
            })
            .ToListAsync(cancellationToken);

        return liveClasses;
    }
}

