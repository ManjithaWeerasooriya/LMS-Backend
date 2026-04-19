namespace LMS_Backend.Models.Entities;

public enum MeetingType
{
    Room = 1,
    Group = 2,
    Teams = 3
}

public enum LiveSessionStatus
{
    Scheduled = 1,
    Live = 2,
    Ended = 3,
    Cancelled = 4
}

public enum AttendanceStatus
{
    Pending = 1,
    Present = 2,
    Late = 3,
    LeftEarly = 4,
    Absent = 5
}

public enum LiveSessionRecordingStatus
{
    NotStarted = 1,
    InProgress = 2,
    Available = 3,
    Failed = 4
}
