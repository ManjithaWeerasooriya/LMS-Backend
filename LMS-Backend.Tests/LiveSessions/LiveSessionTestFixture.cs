using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.LiveSessions;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LMS_Backend.Tests.LiveSessions;

internal sealed class LiveSessionTestFixture : IAsyncDisposable
{
    private LiveSessionTestFixture(ApplicationDBContext context)
    {
        Context = context;

        AzureLiveSessionServiceMock
            .Setup(service => service.CreateRoomAsync(
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("room-generated");

        AzureLiveSessionServiceMock
            .Setup(service => service.UpdateRoomAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AzureLiveSessionServiceMock
            .Setup(service => service.CreateChatThreadAsync(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("chat-thread-generated");

        AzureLiveSessionServiceMock
            .Setup(service => service.EnsureChatParticipantAsync(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AzureLiveSessionServiceMock
            .Setup(service => service.StartRecordingAsync(
                It.IsAny<LiveSession>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession session, CancellationToken _) => new AcsLiveSessionRecordingResult
            {
                AcsRecordingId = $"rec-{session.Id:N}",
                RecordingState = "active",
                RecordedAt = DateTime.UtcNow
            });

        AzureLiveSessionServiceMock
            .Setup(service => service.StopRecordingAsync(
                It.IsAny<LiveSession>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession session, CancellationToken _) => new AcsLiveSessionRecordingResult
            {
                AcsRecordingId = session.AcsRecordingId ?? $"rec-{session.Id:N}",
                RecordingState = "inactive",
                RecordedAt = DateTime.UtcNow
            });

        AzureIdentityServiceMock
            .Setup(service => service.CreateJoinTokenAsync(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, string displayName, bool limitToJoinOnly, CancellationToken _) => new AcsIdentityTokenResult
            {
                AcsUserId = user.AcsIdentityId ?? $"acs-{user.Id}",
                Token = limitToJoinOnly ? "limited-token" : "full-token",
                DisplayName = displayName,
                Endpoint = "https://acs.example.test"
            });
    }

    public ApplicationDBContext Context { get; }

    public Mock<IAzureCommunicationLiveSessionService> AzureLiveSessionServiceMock { get; } = new();

    public Mock<IAzureCommunicationIdentityService> AzureIdentityServiceMock { get; } = new();

    public User Teacher { get; private set; } = default!;

    public User OtherTeacher { get; private set; } = default!;

    public User EnrolledStudent { get; private set; } = default!;

    public User SecondEnrolledStudent { get; private set; } = default!;

    public User OutsiderStudent { get; private set; } = default!;

    public Course OwnedCourse { get; private set; } = default!;

    public Course OtherTeachersCourse { get; private set; } = default!;

    public static async Task<LiveSessionTestFixture> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDBContext(options);
        var fixture = new LiveSessionTestFixture(context);
        await fixture.SeedAsync();
        return fixture;
    }

    public LiveSessionService CreateLiveSessionService()
    {
        return new LiveSessionService(Context, AzureLiveSessionServiceMock.Object);
    }

    public LiveSessionJoinService CreateJoinService()
    {
        return new LiveSessionJoinService(
            Context,
            AzureIdentityServiceMock.Object,
            AzureLiveSessionServiceMock.Object);
    }

    public CreateLiveSessionRequestDto CreateRequest(
        string title = "Weekly live session",
        DateTime? startTime = null,
        bool recordingEnabled = false,
        bool playbackEnabled = false,
        string? chatThreadId = null)
    {
        return new CreateLiveSessionRequestDto
        {
            Title = title,
            Description = "Session description",
            StartTime = startTime ?? DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60,
            RecordingEnabled = recordingEnabled,
            PlaybackEnabled = playbackEnabled,
            ChatThreadId = chatThreadId
        };
    }

    public UpdateLiveSessionRequestDto CreateUpdateRequest(
        string title = "Updated live session",
        DateTime? startTime = null,
        bool recordingEnabled = false,
        bool playbackEnabled = false,
        string? chatThreadId = "chat-thread-updated")
    {
        return new UpdateLiveSessionRequestDto
        {
            Title = title,
            Description = "Updated description",
            StartTime = startTime ?? DateTime.UtcNow.AddDays(2),
            DurationMinutes = 90,
            RecordingEnabled = recordingEnabled,
            PlaybackEnabled = playbackEnabled,
            ChatThreadId = chatThreadId
        };
    }

    public async Task<LiveSession> SeedSessionAsync(
        Course? course = null,
        User? teacher = null,
        LiveSessionStatus status = LiveSessionStatus.Scheduled,
        bool recordingEnabled = false,
        bool playbackEnabled = false,
        LiveSessionRecordingStatus recordingStatus = LiveSessionRecordingStatus.NotStarted,
        string? roomId = "room-123",
        string? chatThreadId = "chat-thread-123",
        string? recordingUrl = null,
        string? acsRecordingId = null,
        DateTime? startTime = null)
    {
        var owner = teacher ?? Teacher;
        var targetCourse = course ?? OwnedCourse;
        var scheduledStart = startTime ?? DateTime.UtcNow.AddHours(-1);

        var session = new LiveSession
        {
            CourseId = targetCourse.Id,
            Course = targetCourse,
            Title = $"{targetCourse.Title} session",
            Description = "Fixture session",
            StartTime = scheduledStart,
            DurationMinutes = 60,
            Status = status,
            RecordingEnabled = recordingEnabled,
            PlaybackEnabled = playbackEnabled,
            RecordingStatus = recordingStatus,
            MeetingType = MeetingType.Room,
            RoomId = roomId,
            ChatThreadId = chatThreadId,
            RecordingUrl = recordingUrl,
            AcsRecordingId = acsRecordingId,
            CreatedByTeacherId = owner.Id,
            CreatedByTeacher = owner,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        Context.LiveSessions.Add(session);
        await Context.SaveChangesAsync();

        return session;
    }

    public async Task EnrollStudentAsync(User student, Course? course = null)
    {
        var targetCourse = course ?? OwnedCourse;
        var alreadyEnrolled = await Context.CourseEnrollments
            .AnyAsync(enrollment => enrollment.CourseId == targetCourse.Id && enrollment.StudentId == student.Id);

        if (alreadyEnrolled)
        {
            return;
        }

        Context.CourseEnrollments.Add(new CourseEnrollment
        {
            CourseId = targetCourse.Id,
            Course = targetCourse,
            StudentId = student.Id,
            Student = student,
            EnrolledAt = DateTime.UtcNow
        });

        await Context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private async Task SeedAsync()
    {
        Teacher = CreateUser("teacher-1", "Ada", "Lovelace", "teacher1@example.com");
        OtherTeacher = CreateUser("teacher-2", "Grace", "Hopper", "teacher2@example.com");
        EnrolledStudent = CreateUser("student-1", "Alice", "Student", "student1@example.com");
        SecondEnrolledStudent = CreateUser("student-2", "Bob", "Student", "student2@example.com");
        OutsiderStudent = CreateUser("student-99", "Oscar", "Outsider", "outsider@example.com");

        OwnedCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Distributed Systems",
            TeacherId = Teacher.Id,
            Teacher = Teacher,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        OtherTeachersCourse = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Cloud Fundamentals",
            TeacherId = OtherTeacher.Id,
            Teacher = OtherTeacher,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        };

        Context.Users.AddRange(
            Teacher,
            OtherTeacher,
            EnrolledStudent,
            SecondEnrolledStudent,
            OutsiderStudent);

        Context.Courses.AddRange(OwnedCourse, OtherTeachersCourse);
        Context.CourseEnrollments.Add(new CourseEnrollment
        {
            CourseId = OwnedCourse.Id,
            Course = OwnedCourse,
            StudentId = EnrolledStudent.Id,
            Student = EnrolledStudent,
            EnrolledAt = DateTime.UtcNow.AddDays(-7)
        });

        await Context.SaveChangesAsync();
    }

    private static User CreateUser(string id, string firstName, string lastName, string email)
    {
        return new User
        {
            Id = id,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
    }
}
