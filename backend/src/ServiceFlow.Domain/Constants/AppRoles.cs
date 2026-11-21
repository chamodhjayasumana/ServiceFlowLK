namespace ServiceFlow.Domain.Constants;

public static class AppRoles
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";
    public const string Technician = "Technician";

    public static readonly IReadOnlyList<string> All =
    [
        Owner,
        Manager,
        Receptionist,
        Technician
    ];

    /// <summary>
    /// Roles that may be assigned via employee invitation. Owner is created only through registration.
    /// </summary>
    public static readonly IReadOnlyList<string> AssignableByInvitation =
    [
        Manager,
        Receptionist,
        Technician
    ];

    public static bool IsAssignableByInvitation(string role) =>
        AssignableByInvitation.Contains(role, StringComparer.OrdinalIgnoreCase);
}
