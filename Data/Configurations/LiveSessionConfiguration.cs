using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("LiveSessions");

        builder.Property(s => s.Title)
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(4000);

        builder.Property(s => s.AcsRoomId)
            .HasMaxLength(200);

        builder.Property(s => s.AcsCallLocator)
            .HasMaxLength(500);

        builder.Property(s => s.ChatThreadId)
            .HasMaxLength(200);

        builder.Property(s => s.AcsRecordingId)
            .HasMaxLength(300);

        builder.Property(s => s.RecordingUrl)
            .HasMaxLength(1000);

        builder.HasIndex(s => new { s.CourseId, s.StartTime });
        builder.HasIndex(s => new { s.CreatedByTeacherId, s.StartTime });

        builder.HasOne(s => s.Course)
            .WithMany(c => c.LiveSessions)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.CreatedByTeacher)
            .WithMany(u => u.CreatedLiveSessions)
            .HasForeignKey(s => s.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
