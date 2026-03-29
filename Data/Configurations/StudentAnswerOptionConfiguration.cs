using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class StudentAnswerOptionConfiguration : IEntityTypeConfiguration<StudentAnswerOption>
{
    public void Configure(EntityTypeBuilder<StudentAnswerOption> builder)
    {
        builder.ToTable("StudentAnswerOptions");

        builder.HasIndex(a => new { a.StudentAnswerId, a.QuestionOptionId })
            .IsUnique();

        builder.HasOne(a => a.StudentAnswer)
            .WithMany(answer => answer.SelectedOptions)
            .HasForeignKey(a => a.StudentAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.QuestionOption)
            .WithMany(option => option.SelectedByAnswers)
            .HasForeignKey(a => a.QuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
