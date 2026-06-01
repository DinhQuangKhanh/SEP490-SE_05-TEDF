using System.Collections.Frozen;
using System.Reflection;
using System.Text.RegularExpressions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Common.Services;

/// <summary>
/// Resolves human-readable action names and categories from command type names.
/// Scans assemblies at startup for <see cref="ActionLogAttribute"/> and caches the results.
/// </summary>
public sealed partial class ActionNameResolver
{
    private readonly FrozenDictionary<string, ActionInfo> _map;

    public ActionNameResolver()
    {
        var dict = new Dictionary<string, ActionInfo>();

        var assembly = typeof(ActionNameResolver).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<ActionLogAttribute>();
            if (attr is not null)
            {
                dict[type.Name] = new ActionInfo(attr.DisplayName, attr.Category);
            }
        }

        _map = dict.ToFrozenDictionary();
    }

    /// <summary>
    /// Resolves the display name and category for a given command type name.
    /// Falls back to converting PascalCase to sentence if no attribute is found.
    /// </summary>
    public ActionInfo Resolve(string typeName)
    {
        if (_map.TryGetValue(typeName, out var info))
            return info;

        // Fallback: strip "Command" suffix, PascalCase → sentence
        var cleanName = typeName.EndsWith("Command", StringComparison.Ordinal)
            ? typeName[..^7]
            : typeName;

        var displayName = PascalCaseToSentence(cleanName);
        return new ActionInfo(displayName, "Other");
    }

    private static string PascalCaseToSentence(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return PascalCaseRegex().Replace(input, " $1").Trim();
    }

    [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])")]
    private static partial Regex PascalCaseRegex();
}

public record ActionInfo(string DisplayName, string Category);
