using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable("Quizzes");

        builder.HasQueryFilter(q => !q.IsDeleted);

        builder.Property(q => q.Title)
            .HasMaxLength(200);

        builder.Property(q => q.Description)
            .HasMaxLength(4000);

        builder.Property(q => q.TotalMarks)
            .HasColumnType("decimal(10,2)");

        builder.HasIndex(q => q.CourseId);

        builder.HasOne(q => q.Course)
            .WithMany(c => c.Quizzes)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
