using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class QuestionOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = default!;

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<StudentAnswerOption> SelectedByAnswers { get; set; } = new List<StudentAnswerOption>();
}
