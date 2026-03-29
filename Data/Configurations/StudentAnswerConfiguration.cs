using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.ToTable("StudentAnswers");

        builder.Property(a => a.AnswerText)
            .HasMaxLength(8000);

        builder.Property(a => a.FileReference)
            .HasMaxLength(1000);

        builder.Property(a => a.TeacherFeedback)
            .HasMaxLength(2000);

        builder.Property(a => a.AwardedMarks)
            .HasColumnType("decimal(10,2)");

        builder.HasIndex(a => new { a.QuizAttemptId, a.QuestionId })
            .IsUnique();

        builder.HasOne(a => a.QuizAttempt)
            .WithMany(attempt => attempt.Answers)
            .HasForeignKey(a => a.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
