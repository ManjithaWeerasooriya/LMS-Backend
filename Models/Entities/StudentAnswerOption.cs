using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class StudentAnswerOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StudentAnswerId { get; set; }

    [ForeignKey(nameof(StudentAnswerId))]
    public StudentAnswer StudentAnswer { get; set; } = default!;

    [Required]
    public Guid QuestionOptionId { get; set; }

    [ForeignKey(nameof(QuestionOptionId))]
    public QuestionOption QuestionOption { get; set; } = default!;
}
