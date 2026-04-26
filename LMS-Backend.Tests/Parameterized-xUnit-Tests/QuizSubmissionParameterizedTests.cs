using System.ComponentModel.DataAnnotations;
using LMS_Backend.Models.DTOs.Quiz;

namespace LMS_Backend.Tests.ParameterizedXUnitTests;

// Parameterized tests for quiz answer payload validation rules
public class QuizSubmissionParameterizedTests
{
    // Validates combinations of options, text, and file inputs using [Theory]
    [Theory]
    [InlineData(0, null, null, true)]
    [InlineData(0, "Essay answer", null, true)]
    [InlineData(0, null, "submission.pdf", true)]
    [InlineData(1, null, null, true)]
    [InlineData(1, "Essay answer", null, false)]
    [InlineData(1, null, "submission.pdf", false)]
    public void SubmitStudentAnswerDto_Validate_ShouldReturnExpectedResult(
        int selectedOptionCount,
        string? answerText,
        string? fileReference,
        bool expectedIsValid)
    {
        // Arrange
        var dto = new SubmitStudentAnswerDto
        {
            QuestionId = Guid.NewGuid(),
            SelectedOptionIds = selectedOptionCount == 0
                ? null
                : Enumerable.Range(0, selectedOptionCount).Select(_ => Guid.NewGuid()).ToList(),
            AnswerText = answerText,
            FileReference = fileReference
        };

        // Act
        var validationResults = Validate(dto);
        var isValid = validationResults.Count == 0;

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    // Helper method to perform DataAnnotations validation on DTO
    private static List<ValidationResult> Validate(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        Validator.TryValidateObject(dto, context, validationResults, validateAllProperties: true);
        return validationResults;
    }
}
