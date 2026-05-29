namespace TEDF.Application.Common.Attributes;

/// <summary>
/// Marks a command with a human-readable display name and category for activity logging.
/// Commands without this attribute will have their name auto-generated from the class name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ActionLogAttribute : Attribute
{
    public string DisplayName { get; }
    public string Category { get; }

    public ActionLogAttribute(string displayName, string category)
    {
        DisplayName = displayName;
        Category = category;
    }
}
