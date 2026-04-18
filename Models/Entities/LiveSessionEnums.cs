namespace LMS_Backend.Models.Entities;

public enum LiveSessionStatus
{
    Scheduled = 1,
    Active = 2,
    Completed = 3,
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
