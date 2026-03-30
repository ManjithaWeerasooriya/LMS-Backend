using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Data;

public class ApplicationDBContext : IdentityDbContext<User>
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<LiveClass> LiveClasses => Set<LiveClass>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Material> Materials => Set<Material>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique constraint on UserId + DeviceId to ensure one refresh token per device per user
        builder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique();

        builder.Entity<CourseEnrollment>()
            .HasIndex(x => new { x.CourseId, x.StudentId })
            .IsUnique();

        // Configure relationships to avoid multiple cascade paths in SQL Server.
        builder.Entity<Course>()
            .HasOne(c => c.Teacher)
            .WithMany()
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CourseEnrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CourseEnrollment>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Quiz>()
            .HasOne(q => q.Course)
            .WithMany(c => c.Quizzes)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>()
            .HasOne(a => a.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LiveClass>()
            .HasOne(l => l.Teacher)
            .WithMany()
            .HasForeignKey(l => l.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LiveClass>()
            .HasOne(l => l.Course)
            .WithMany(c => c.LiveClasses)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Assignment>()
            .HasOne(a => a.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AssignmentSubmission>()
            .HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AssignmentSubmission>()
            .HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Material config
        builder.Entity<Material>()
            .HasKey(m => m.Id);

        builder.Entity<Material>()
            .Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Material>()
            .Property(m => m.FileUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Entity<Material>()
            .Property(m => m.BlobName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Entity<Material>()
            .Property(m => m.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Material>()
            .Property(m => m.MaterialType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Entity<Material>()
            .Property(m => m.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Entity<Material>()
            .HasOne(m => m.Course)
            .WithMany()
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Material>()
            .HasIndex(m => m.CourseId);
    }
}