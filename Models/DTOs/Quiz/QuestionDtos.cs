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

    public List<QuestionOptionRequestDto>? Options { get; set; }

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

    public List<QuestionOptionRequestDto>? Options { get; set; }

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
    private const string OptionsMemberName = nameof(CreateQuestionDto.Options);

    public static IEnumerable<ValidationResult> Validate(
        QuestionType type,
        IReadOnlyCollection<QuestionOptionRequestDto>? options)
    {
        var normalizedOptions = options ?? [];

        if (IsObjective(type))
        {
            if (type == QuestionType.TrueFalse)
            {
                if (normalizedOptions.Count != 2)
                {
                    yield return CreateOptionsValidationResult(
                        "True/false questions must define exactly two options.");
                }
            }
            else if (normalizedOptions.Count < 2)
            {
                yield return CreateOptionsValidationResult(
                    GetMinimumOptionsMessage(type));
            }

            var distinctOrderIndexes = normalizedOptions.Select(o => o.OrderIndex).Distinct().Count();
            if (distinctOrderIndexes != normalizedOptions.Count)
            {
                yield return CreateOptionsValidationResult(
                    "Question option order indexes must be unique.",
                    new[] { OptionsMemberName });
            }

            var correctCount = normalizedOptions.Count(o => o.IsCorrect);
            if (type == QuestionType.SingleMcq && normalizedOptions.Count >= 2 && correctCount != 1)
            {
                yield return CreateOptionsValidationResult(
                    "Single choice questions must have exactly one correct option.");
            }

            if (type == QuestionType.MultipleMcq && normalizedOptions.Count >= 2 && correctCount == 0)
            {
                yield return CreateOptionsValidationResult(
                    "Multiple choice questions must have at least one correct option.",
                    new[] { OptionsMemberName });
            }

            if (type == QuestionType.TrueFalse && normalizedOptions.Count == 2 && correctCount != 1)
            {
                yield return CreateOptionsValidationResult(
                    "True/false questions must have exactly one correct option.");
            }
        }
        else if (normalizedOptions.Count > 0)
        {
            yield return CreateOptionsValidationResult(
                GetSubjectiveOptionsMessage(type));
        }
    }

    public static bool IsObjective(QuestionType type) =>
        type == QuestionType.SingleMcq ||
        type == QuestionType.MultipleMcq ||
        type == QuestionType.TrueFalse;

    private static ValidationResult CreateOptionsValidationResult(
        string message,
        string[]? memberNames = null) =>
        new(message, memberNames ?? [OptionsMemberName]);

    private static string GetMinimumOptionsMessage(QuestionType type) =>
        type switch
        {
            QuestionType.SingleMcq => "Single choice questions must define at least two options.",
            QuestionType.MultipleMcq => "Multiple choice questions must define at least two options.",
            _ => "Objective questions must define at least two options."
        };

    private static string GetSubjectiveOptionsMessage(QuestionType type) =>
        type switch
        {
            QuestionType.Essay => "Essay questions must not define options.",
            QuestionType.ShortAnswer => "Short answer questions must not define options.",
            QuestionType.FileUpload => "File upload questions must not define options.",
            _ => "This question type must not define options."
        };
}
