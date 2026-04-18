namespace LMS_Backend.Infrastructure.Auth;

public static class AppRoles
{
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string LegacyAdmin = "Admin";

    public static bool TryNormalizeRequestedRole(string? value, out string normalizedRole)
    {
        if (string.Equals(value?.Trim(), Teacher, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = Teacher;
            return true;
        }

        if (string.Equals(value?.Trim(), Student, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = Student;
            return true;
        }

        normalizedRole = string.Empty;
        return false;
    }

    public static string ResolveSystemRole(IEnumerable<string> roles)
    {
        var roleSet = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains(Teacher) || roleSet.Contains(LegacyAdmin))
        {
            return Teacher;
        }

        return Student;
    }
}

public static class AppPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string TeacherOnly = nameof(TeacherOnly);
    public const string StudentOnly = nameof(StudentOnly);
}

public static class AppClaimTypes
{
    public const string Role = "role";
}
