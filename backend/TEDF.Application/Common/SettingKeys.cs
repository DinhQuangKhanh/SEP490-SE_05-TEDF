namespace TEDF.Application.Common;

/// <summary>
/// Canonical keys for rows in the SystemConfiguration store. Use these constants everywhere
/// (handlers, middleware, seed data) instead of magic strings.
/// </summary>
public static class SettingKeys
{
    // Registration rules
    public const string MaxGroupMembers = "MaxGroupMembers";           // Int (existing seed id 2)
    public const string MaxTopicsPerMentor = "MaxTopicsPerMentor";     // Int
    public const string RequireOutlineApproval = "RequireOutlineApproval";   // Bool

    // Appearance / branding (publicly readable)
    public const string PrimaryColor = "PrimaryColor";                 // String
    public const string HeaderName = "HeaderName";                     // String
    public const string LogoUrl = "LogoUrl";                           // String

    // System
    public const string MaintenanceMode = "MaintenanceMode";          // Bool

    // Notifications
    public const string EmailOnEvaluationResult = "EmailOnEvaluationResult";       // Bool
    public const string NotifyMentorOnRegistration = "NotifyMentorOnRegistration"; // Bool

    /// <summary>Keys exposed by the anonymous /api/settings/public endpoint (no secrets).</summary>
    public static readonly string[] PublicKeys =
    [
        PrimaryColor, HeaderName, LogoUrl, MaintenanceMode
    ];
}
