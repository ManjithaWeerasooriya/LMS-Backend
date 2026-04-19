using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LMS_Backend.Tests.LiveSessions;

public class LiveSessionServiceTests
{
    [Fact]
    public async Task CreateLiveSessionAsync_CreatesSessionForCourseOwner_AndRejectsOtherTeachers()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var request = fixture.CreateRequest(chatThreadId: null);

        var created = await service.CreateLiveSessionAsync(
            fixture.Teacher.Id,
            fixture.OwnedCourse.Id,
            request,
            CancellationToken.None);

        Assert.Equal(fixture.OwnedCourse.Id, created.CourseId);
        Assert.Equal(fixture.Teacher.Id, created.CreatedByTeacherId);
        Assert.Equal(LiveSessionStatus.Scheduled, created.Status);
        Assert.Equal(MeetingType.Room, created.MeetingType);
        Assert.Equal("room-generated", created.RoomId);
        Assert.Equal("chat-thread-generated", created.ChatThreadId);

        fixture.AzureLiveSessionServiceMock.Verify(liveSessionService => liveSessionService.CreateRoomAsync(
            request.StartTime,
            request.DurationMinutes,
            It.IsAny<CancellationToken>()), Times.Once);

        var stored = await fixture.Context.LiveSessions.SingleAsync();
        Assert.Equal(created.Id, stored.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateLiveSessionAsync(
            fixture.OtherTeacher.Id,
            fixture.OwnedCourse.Id,
            request,
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateLiveSessionAsync_UpdatesOwnedSession_AndRejectsOtherTeachers()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync();
        var update = fixture.CreateUpdateRequest(recordingEnabled: true, playbackEnabled: true);

        var updated = await service.UpdateLiveSessionAsync(
            fixture.Teacher.Id,
            session.Id,
            update,
            CancellationToken.None);

        Assert.Equal("Updated live session", updated.Title);
        Assert.True(updated.RecordingEnabled);
        Assert.True(updated.PlaybackEnabled);
        Assert.Equal(MeetingType.Room, updated.MeetingType);
        Assert.Equal("room-123", updated.RoomId);
        Assert.Equal("chat-thread-updated", updated.ChatThreadId);

        fixture.AzureLiveSessionServiceMock.Verify(liveSessionService => liveSessionService.UpdateRoomAsync(
            "room-123",
            update.StartTime,
            update.DurationMinutes,
            It.IsAny<CancellationToken>()), Times.Once);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateLiveSessionAsync(
            fixture.OtherTeacher.Id,
            session.Id,
            update,
            CancellationToken.None));
    }

    [Fact]
    public async Task CancelLiveSessionAsync_CancelsOwnedSession_AndRejectsOtherTeachers()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync();

        await service.CancelLiveSessionAsync(
            fixture.Teacher.Id,
            session.Id,
            CancellationToken.None);

        var stored = await fixture.Context.LiveSessions.SingleAsync(liveSession => liveSession.Id == session.Id);
        Assert.Equal(LiveSessionStatus.Cancelled, stored.Status);
        Assert.NotNull(stored.UpdatedAt);

        var anotherSession = await fixture.SeedSessionAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelLiveSessionAsync(
            fixture.OtherTeacher.Id,
            anotherSession.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task GetStudentLiveSessionByIdAsync_AllowsEnrolledStudents_AndRejectsOutsiders()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync();

        var visible = await service.GetStudentLiveSessionByIdAsync(
            fixture.EnrolledStudent.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(session.Id, visible.Id);
        Assert.Equal(fixture.OwnedCourse.Title, visible.CourseTitle);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetStudentLiveSessionByIdAsync(
            fixture.OutsiderStudent.Id,
            session.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task StartAndEndLiveSessionAsync_TransitionsScheduledToLiveToEnded()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync(chatThreadId: null, status: LiveSessionStatus.Scheduled);

        fixture.Context.LiveSessionAttendances.Add(new LiveSessionAttendance
        {
            SessionId = session.Id,
            Session = session,
            StudentId = fixture.EnrolledStudent.Id,
            Student = fixture.EnrolledStudent,
            JoinTime = DateTime.UtcNow.AddMinutes(-12),
            AttendanceStatus = AttendanceStatus.Present
        });
        await fixture.Context.SaveChangesAsync();

        var started = await service.StartLiveSessionAsync(
            fixture.Teacher.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(LiveSessionStatus.Live, started.Status);
        Assert.Equal("chat-thread-generated", started.ChatThreadId);

        var ended = await service.EndLiveSessionAsync(
            fixture.Teacher.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(LiveSessionStatus.Ended, ended.Status);

        var attendance = await fixture.Context.LiveSessionAttendances.SingleAsync(record => record.SessionId == session.Id);
        Assert.NotNull(attendance.LeaveTime);
        Assert.True(attendance.DurationSeconds > 0);
        Assert.NotNull(attendance.LastSeenAt);
    }

    [Fact]
    public async Task StartAndEndLiveSessionAsync_OnlyOwningTeacherCanChangeStatus()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var scheduledSession = await fixture.SeedSessionAsync(status: LiveSessionStatus.Scheduled);
        var liveSession = await fixture.SeedSessionAsync(status: LiveSessionStatus.Live, chatThreadId: "chat-thread-123");

        await Assert.ThrowsAsync<ForbiddenException>(() => service.StartLiveSessionAsync(
            fixture.OtherTeacher.Id,
            scheduledSession.Id,
            CancellationToken.None));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.EndLiveSessionAsync(
            fixture.OtherTeacher.Id,
            liveSession.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task StartRecordingAsync_RequiresRecordingEnabledAndLiveSession()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var scheduledSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Scheduled,
            recordingEnabled: true);
        var disabledSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            recordingEnabled: false);
        var recordableSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            recordingEnabled: true,
            playbackEnabled: true);

        await Assert.ThrowsAsync<ConflictException>(() => service.StartRecordingAsync(
            fixture.Teacher.Id,
            scheduledSession.Id,
            CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() => service.StartRecordingAsync(
            fixture.Teacher.Id,
            disabledSession.Id,
            CancellationToken.None));

        var recording = await service.StartRecordingAsync(
            fixture.Teacher.Id,
            recordableSession.Id,
            CancellationToken.None);

        Assert.Equal(recordableSession.Id, recording.SessionId);
        Assert.Equal(LiveSessionRecordingStatus.InProgress, recording.RecordingStatus);
        Assert.NotNull(recording.AcsRecordingId);
        Assert.NotNull(recording.RecordingStartedAt);
    }

    [Fact]
    public async Task GetStudentRecordingAsync_RequiresEnrollmentAndPlaybackPermission()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var playableSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Ended,
            recordingEnabled: true,
            playbackEnabled: true,
            recordingStatus: LiveSessionRecordingStatus.Available,
            recordingUrl: "https://recordings.example.test/session.mp4",
            acsRecordingId: "rec-001");
        var restrictedSession = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Ended,
            recordingEnabled: true,
            playbackEnabled: false,
            recordingStatus: LiveSessionRecordingStatus.Available,
            recordingUrl: "https://recordings.example.test/restricted.mp4",
            acsRecordingId: "rec-002");

        var recording = await service.GetStudentRecordingAsync(
            fixture.EnrolledStudent.Id,
            playableSession.Id,
            CancellationToken.None);

        Assert.Equal(playableSession.Id, recording.SessionId);
        Assert.Equal("https://recordings.example.test/session.mp4", recording.RecordingUrl);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetStudentRecordingAsync(
            fixture.EnrolledStudent.Id,
            restrictedSession.Id,
            CancellationToken.None));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetStudentRecordingAsync(
            fixture.OutsiderStudent.Id,
            playableSession.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task JoinAttendanceAsync_CreatesAttendanceRecord()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync(
            status: LiveSessionStatus.Live,
            startTime: DateTime.UtcNow.AddMinutes(-1));

        var attendance = await service.JoinAttendanceAsync(
            fixture.EnrolledStudent.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(session.Id, attendance.SessionId);
        Assert.Equal(fixture.EnrolledStudent.Id, attendance.StudentId);
        Assert.NotNull(attendance.JoinTime);
        Assert.Equal(AttendanceStatus.Present, attendance.AttendanceStatus);

        var stored = await fixture.Context.LiveSessionAttendances.SingleAsync(record => record.SessionId == session.Id);
        Assert.Equal(fixture.EnrolledStudent.Id, stored.StudentId);
    }

    [Fact]
    public async Task LeaveAttendanceAsync_UpdatesLeaveTimeAndDuration()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        var session = await fixture.SeedSessionAsync(status: LiveSessionStatus.Live);

        fixture.Context.LiveSessionAttendances.Add(new LiveSessionAttendance
        {
            SessionId = session.Id,
            Session = session,
            StudentId = fixture.EnrolledStudent.Id,
            Student = fixture.EnrolledStudent,
            JoinTime = DateTime.UtcNow.AddMinutes(-10),
            AttendanceStatus = AttendanceStatus.Present
        });
        await fixture.Context.SaveChangesAsync();

        var attendance = await service.LeaveAttendanceAsync(
            fixture.EnrolledStudent.Id,
            session.Id,
            CancellationToken.None);

        Assert.NotNull(attendance.LeaveTime);
        Assert.True(attendance.DurationSeconds >= 600);
        Assert.NotNull(attendance.LastSeenAt);
    }

    [Fact]
    public async Task GetLiveSessionAttendanceSummaryAsync_ReturnsAttendanceOverviewForTeacher()
    {
        await using var fixture = await LiveSessionTestFixture.CreateAsync();
        var service = fixture.CreateLiveSessionService();
        await fixture.EnrollStudentAsync(fixture.SecondEnrolledStudent);

        var session = await fixture.SeedSessionAsync(status: LiveSessionStatus.Ended);
        fixture.Context.LiveSessionAttendances.Add(new LiveSessionAttendance
        {
            SessionId = session.Id,
            Session = session,
            StudentId = fixture.EnrolledStudent.Id,
            Student = fixture.EnrolledStudent,
            JoinTime = session.StartTime.AddMinutes(1),
            LeaveTime = session.StartTime.AddMinutes(55),
            DurationSeconds = 54 * 60,
            AttendanceStatus = AttendanceStatus.Present,
            LastSeenAt = session.StartTime.AddMinutes(55)
        });
        await fixture.Context.SaveChangesAsync();

        var summary = await service.GetLiveSessionAttendanceSummaryAsync(
            fixture.Teacher.Id,
            session.Id,
            CancellationToken.None);

        Assert.Equal(session.Id, summary.SessionId);
        Assert.Equal(2, summary.TotalEnrolledStudents);
        Assert.Equal(1, summary.TotalJoinedStudents);
        Assert.Equal(1, summary.TotalCompletedAttendances);
        Assert.Equal(2, summary.Students.Count);
        Assert.Contains(summary.Students, student => student.StudentId == fixture.EnrolledStudent.Id && student.JoinTime.HasValue);
        Assert.Contains(summary.Students, student => student.StudentId == fixture.SecondEnrolledStudent.Id && student.AttendanceStatus == AttendanceStatus.Absent);
    }
}
