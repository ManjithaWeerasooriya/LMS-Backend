using LMS_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS_Backend.Data.Configurations;

public class LiveSessionAttendanceConfiguration : IEntityTypeConfiguration<LiveSessionAttendance>
{
    public void Configure(EntityTypeBuilder<LiveSessionAttendance> builder)
    {
        builder.ToTable("LiveSessionAttendances");

        builder.HasIndex(a => new { a.SessionId, a.StudentId })
            .IsUnique();

        builder.HasIndex(a => a.StudentId);
        builder.HasIndex(a => a.LastSeenAt);

        builder.HasOne(a => a.Session)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Student)
            .WithMany(u => u.LiveSessionAttendances)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
