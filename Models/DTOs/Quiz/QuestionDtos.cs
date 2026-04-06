using System.ComponentModel.DataAnnotations;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Models.DTOs.Quiz;

public class QuestionOptionRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }
}

public class CreateQuestionDto : IValidatableObject
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public QuestionType Type { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal Marks { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }

    public List<QuestionOptionRequestDto> Options { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in QuestionValidation.Validate(Type, Options))
        {
            yield return validationResult;
        }
    }
}

public class UpdateQuestionDto : IValidatableObject
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public QuestionType Type { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal Marks { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }

    public List<QuestionOptionRequestDto> Options { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in QuestionValidation.Validate(Type, Options))
        {
            yield return validationResult;
        }
    }
}

public class QuestionResponseDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Marks { get; set; }
    public int OrderIndex { get; set; }
    public List<QuestionOptionResponseDto> Options { get; set; } = new();
}

public class QuestionOptionResponseDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

internal static class QuestionValidation
{
    public static IEnumerable<ValidationResult> Validate(
        QuestionType type,
        List<QuestionOptionRequestDto> options)
    {
        if (IsObjective(type))
        {
            if (options.Count < 2)
            {
                yield return new ValidationResult(
                    "Objective questions require at least two options.",
                    new[] { nameof(CreateQuestionDto.Options) });
            }

            var distinctOrderIndexes = options.Select(o => o.OrderIndex).Distinct().Count();
            if (distinctOrderIndexes != options.Count)
            {
                yield return new ValidationResult(
                    "Question option order indexes must be unique.",
                    new[] { nameof(CreateQuestionDto.Options) });
            }

            var correctCount = options.Count(o => o.IsCorrect);
            if (type == QuestionType.SingleMcq || type == QuestionType.TrueFalse)
            {
                if (correctCount != 1)
                {
                    yield return new ValidationResult(
                        "Single choice and true/false questions must have exactly one correct option.",
                        new[] { nameof(CreateQuestionDto.Options) });
                }
            }

            if (type == QuestionType.MultipleMcq && correctCount == 0)
            {
                yield return new ValidationResult(
                    "Multiple choice questions must have at least one correct option.",
                    new[] { nameof(CreateQuestionDto.Options) });
            }

            if (type == QuestionType.TrueFalse && options.Count != 2)
            {
                yield return new ValidationResult(
                    "True/false questions must have exactly two options.",
                    new[] { nameof(CreateQuestionDto.Options) });
            }
        }
        else if (options.Count > 0)
        {
            yield return new ValidationResult(
                "Subjective questions must not define options.",
                new[] { nameof(CreateQuestionDto.Options) });
        }
    }

    public static bool IsObjective(QuestionType type) =>
        type == QuestionType.SingleMcq ||
        type == QuestionType.MultipleMcq ||
        type == QuestionType.TrueFalse;
}
