using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasQueryFilter(q => !q.IsDeleted);

        builder.Property(q => q.Text)
            .HasMaxLength(4000);

        builder.Property(q => q.Marks)
            .HasColumnType("decimal(10,2)");

        builder.HasIndex(q => new { q.QuizId, q.OrderIndex });

        builder.HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
