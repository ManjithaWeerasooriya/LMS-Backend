using System;
using System.Collections.Generic;

namespace LMS_Backend.Models.DTOs.Courses;

public class CourseDiscussionMessageDto
{
    public Guid Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CourseDiscussionMessageDto> Replies { get; set; } = new();
}

public class CreateCourseDiscussionMessageDto
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentMessageId { get; set; }
}

