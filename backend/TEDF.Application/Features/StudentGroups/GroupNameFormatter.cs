using TEDF.Domain.Enums.Project;

namespace TEDF.Application.Features.StudentGroups;

/// <summary>
/// Builds the name shown for a group. A group is known only by its id (SE_NN) until its topic
/// passes evaluation; from then on it is presented as "SE_NN - English topic name - Mentor".
/// </summary>
public static class GroupNameFormatter
{
    /// <summary>
    /// Composes the display name for a group.
    /// </summary>
    /// <param name="groupName">The group id, e.g. SE_01 (falls back to the full group code).</param>
    /// <param name="groupCode">The full group code, e.g. SUMMER2026-SE_01.</param>
    /// <param name="projectNameEn">The topic's English name, when it has a topic.</param>
    /// <param name="mentorName">The supervising mentor's full name.</param>
    /// <param name="projectStatus">The topic's status; the composed name appears once it is approved.</param>
    public static string Build(
        string? groupName,
        string groupCode,
        string? projectNameEn,
        string? mentorName,
        string? projectStatus)
    {
        var id = string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;

        if (!IsEvaluated(projectStatus)
            || string.IsNullOrWhiteSpace(projectNameEn)
            || string.IsNullOrWhiteSpace(mentorName))
        {
            return id;
        }

        return $"{id} - {projectNameEn} - {mentorName}";
    }

    /// <summary>
    /// True once the topic has cleared evaluation — the point at which the group earns its name.
    /// </summary>
    private static bool IsEvaluated(string? projectStatus) =>
        projectStatus == nameof(ProjectStatus.Approved)
        || projectStatus == nameof(ProjectStatus.InProgress)
        || projectStatus == nameof(ProjectStatus.Completed);
}
