using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Courses;
using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class CourseDiscussionService
{
    private readonly ApplicationDBContext _dbContext;

    public CourseDiscussionService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsStudentEnrolledInCourseAsync(
        Guid courseId,
        string studentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CourseEnrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId, cancellationToken);
    }

    public async Task<bool> IsTeacherOwnerOfCourseAsync(
        Guid courseId,
        string teacherId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Courses
            .AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId, cancellationToken);
    }

    public async Task<List<CourseDiscussionMessageDto>> GetDiscussionForCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var messages = await _dbContext.CourseDiscussionMessages
            .Include(m => m.Student)
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var lookup = messages.ToLookup(m => m.ParentMessageId);

        List<CourseDiscussionMessageDto> BuildThread(Guid? parentId)
        {
            return lookup[parentId]
                .OrderBy(m => m.CreatedAt)
                .Select(m => new CourseDiscussionMessageDto
                {
                    Id = m.Id,
                    AuthorName = GetDisplayName(m.Student),
                    AuthorInitials = GetInitials(m.Student),
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    Replies = BuildThread(m.Id)
                })
                .ToList();
        }

        return BuildThread(null);
    }

    public async Task<CourseDiscussionMessageDto> CreateMessageAsync(
        Guid courseId,
        string studentId,
        string content,
        Guid? parentMessageId,
        CancellationToken cancellationToken)
    {
        content = content.Trim();

        var message = new CourseDiscussionMessage
        {
            CourseId = courseId,
            StudentId = studentId,
            Content = content,
            ParentMessageId = parentMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CourseDiscussionMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.CourseDiscussionMessages
            .Include(m => m.Student)
            .FirstAsync(m => m.Id == message.Id, cancellationToken);

        return new CourseDiscussionMessageDto
        {
            Id = saved.Id,
            AuthorName = GetDisplayName(saved.Student),
            AuthorInitials = GetInitials(saved.Student),
            Content = saved.Content,
            CreatedAt = saved.CreatedAt,
            Replies = new List<CourseDiscussionMessageDto>()
        };
    }

    private static string GetDisplayName(User user)
    {
        var first = user.FirstName?.Trim();
        var last = user.LastName?.Trim();

        if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last))
        {
            return string.Join(" ", new[] { first, last }.Where(x => !string.IsNullOrEmpty(x)));
        }

        return user.Email ?? "Student";
    }

    private static string GetInitials(User user)
    {
        var initials = "";

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            initials += char.ToUpperInvariant(user.FirstName[0]);
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            initials += char.ToUpperInvariant(user.LastName[0]);
        }

        if (initials.Length == 0 && !string.IsNullOrWhiteSpace(user.Email))
        {
            initials = char.ToUpperInvariant(user.Email[0]).ToString();
        }

        return initials.Length > 0 ? initials : "S";
    }
}
