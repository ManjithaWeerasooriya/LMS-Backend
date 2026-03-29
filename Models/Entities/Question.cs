using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS_Backend.Models.Entities;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid QuizId { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = default!;

    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Marks { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}
