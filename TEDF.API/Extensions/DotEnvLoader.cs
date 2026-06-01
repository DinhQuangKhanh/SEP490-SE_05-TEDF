using System.Text;

namespace TEDF.API.Extensions;

/// <summary>
/// Minimal .env loader for local/on-prem deployments.
/// Loads key-value pairs into process environment variables.
/// Existing environment variables are preserved.
/// </summary>
public static class DotEnvLoader
{
  public static void LoadForCurrentEnvironment(string contentRoot, string environmentName)
  {
    LoadFile(Path.Combine(contentRoot, ".env"));
    LoadFile(Path.Combine(contentRoot, $".env.{environmentName}"));
  }

  private static void LoadFile(string filePath)
  {
    if (!File.Exists(filePath))
    {
      return;
    }

    foreach (var raw in File.ReadAllLines(filePath, Encoding.UTF8))
    {
      var line = raw.Trim();

      if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
      {
        continue;
      }

      var separatorIndex = line.IndexOf('=');
      if (separatorIndex <= 0)
      {
        continue;
      }

      var key = line[..separatorIndex].Trim();
      var value = line[(separatorIndex + 1)..].Trim();

      if (string.IsNullOrWhiteSpace(key))
      {
        continue;
      }

      // Strip optional matching quotes.
      if (value.Length >= 2)
      {
        var first = value[0];
        var last = value[^1];
        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
          value = value[1..^1];
        }
      }

      // Keep existing values from host/process/environment as highest priority.
      if (Environment.GetEnvironmentVariable(key) is null)
      {
        Environment.SetEnvironmentVariable(key, value);
      }
    }
  }
}
