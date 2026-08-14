using TEDF.Domain.Constants;

namespace TEDF.Infrastructure.EventHandlers.Support;

/// <summary>
/// Support pages differ per role, so the link in a ticket email depends on who is reading it.
/// Mirrors <see cref="TicketMessageAddedEventHandler"/>, which picks the same route for the in-app
/// notification — the two must not drift apart or a reader would follow a link into a 403.
/// </summary>
internal static class SupportMailRoutes
{
    // Fully qualified: the sibling EventHandlers.User namespace shadows the aggregate name here.
    public static string ResolveSupportPath(TEDF.Domain.Aggregates.UserAggregate.User? user)
    {
        if (user is null) return "/lecturer/support";
        if (user.HasRole(DomainRoleIds.Student)) return "/student/support";
        if (user.HasRole(DomainRoleIds.Admin)) return "/admin/support";
        return "/lecturer/support";
    }
}
