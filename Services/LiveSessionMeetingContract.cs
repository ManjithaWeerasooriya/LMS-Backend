using System.Text.RegularExpressions;
using LMS_Backend.Models.Entities;

namespace LMS_Backend.Services;

internal static partial class LiveSessionMeetingContract
{
    public static LiveSessionMeetingDetails ValidateAndNormalize(
        MeetingType meetingType,
        string? roomId,
        string? groupId,
        string? meetingLink,
        string? meetingId,
        string? passcode)
    {
        if (!Enum.IsDefined(meetingType))
        {
            throw new ArgumentException("A valid meeting type is required.");
        }

        var normalizedRoomId = NormalizeOptional(roomId);
        var normalizedGroupId = NormalizeOptional(groupId);
        var normalizedMeetingLink = NormalizeOptional(meetingLink);
        var normalizedMeetingId = NormalizeOptional(meetingId);
        var normalizedPasscode = NormalizeOptional(passcode);

        var hasRoom = normalizedRoomId != null;
        var hasGroup = normalizedGroupId != null;
        var hasMeetingLink = normalizedMeetingLink != null;
        var hasMeetingId = normalizedMeetingId != null;
        var hasPasscode = normalizedPasscode != null;

        if (hasMeetingId != hasPasscode)
        {
            throw new ArgumentException("Teams meetingId and passcode must be provided together.");
        }

        var locatorCount = 0;
        locatorCount += hasRoom ? 1 : 0;
        locatorCount += hasGroup ? 1 : 0;
        locatorCount += hasMeetingLink ? 1 : 0;
        locatorCount += hasMeetingId ? 1 : 0;

        if (locatorCount == 0)
        {
            throw new ArgumentException("Exactly one meeting locator is required.");
        }

        if (locatorCount > 1)
        {
            throw new ArgumentException("Provide exactly one meeting locator for the selected meeting type.");
        }

        switch (meetingType)
        {
            case MeetingType.Room:
                if (!hasRoom)
                {
                    throw new ArgumentException("Room meetings require roomId.");
                }

                return new LiveSessionMeetingDetails(
                    MeetingType.Room,
                    normalizedRoomId,
                    null,
                    null,
                    null,
                    null);

            case MeetingType.Group:
                if (!hasGroup)
                {
                    throw new ArgumentException("Group meetings require groupId.");
                }

                if (!Guid.TryParse(normalizedGroupId, out _))
                {
                    throw new ArgumentException("groupId must be a valid GUID.");
                }

                return new LiveSessionMeetingDetails(
                    MeetingType.Group,
                    null,
                    normalizedGroupId,
                    null,
                    null,
                    null);

            case MeetingType.Teams:
                if (hasMeetingLink)
                {
                    if (!Uri.TryCreate(normalizedMeetingLink, UriKind.Absolute, out var meetingUri) ||
                        !string.Equals(meetingUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("meetingLink must be a valid HTTPS URL.");
                    }

                    return new LiveSessionMeetingDetails(
                        MeetingType.Teams,
                        null,
                        null,
                        normalizedMeetingLink,
                        null,
                        null);
                }

                if (hasMeetingId)
                {
                    if (!MeetingIdPattern().IsMatch(normalizedMeetingId!))
                    {
                        throw new ArgumentException("meetingId contains unsupported characters.");
                    }

                    return new LiveSessionMeetingDetails(
                        MeetingType.Teams,
                        null,
                        null,
                        null,
                        normalizedMeetingId,
                        normalizedPasscode);
                }

                throw new ArgumentException("Teams meetings require meetingLink or meetingId and passcode.");

            default:
                throw new ArgumentException("A valid meeting type is required.");
        }
    }

    public static LiveSessionMeetingDetails ValidateAndNormalize(LiveSession session)
    {
        return ValidateAndNormalize(
            session.MeetingType,
            session.RoomId,
            session.GroupId,
            session.MeetingLink,
            session.MeetingId,
            session.Passcode);
    }

    public static void ApplyToSession(LiveSession session, LiveSessionMeetingDetails meetingDetails)
    {
        session.MeetingType = meetingDetails.MeetingType;
        session.RoomId = meetingDetails.RoomId;
        session.GroupId = meetingDetails.GroupId;
        session.MeetingLink = meetingDetails.MeetingLink;
        session.MeetingId = meetingDetails.MeetingId;
        session.Passcode = meetingDetails.Passcode;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 _\.-]*$")]
    private static partial Regex MeetingIdPattern();
}

internal sealed record LiveSessionMeetingDetails(
    MeetingType MeetingType,
    string? RoomId,
    string? GroupId,
    string? MeetingLink,
    string? MeetingId,
    string? Passcode);
