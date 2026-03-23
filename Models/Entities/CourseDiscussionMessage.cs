using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class CourseDiscussionMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public string StudentId { get; set; } = default!;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public Guid? ParentMessageId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = default!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = default!;

    public CourseDiscussionMessage? ParentMessage { get; set; }

    public ICollection<CourseDiscussionMessage> Replies { get; set; } = new List<CourseDiscussionMessage>();
}

