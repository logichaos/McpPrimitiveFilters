namespace McpServer.Infrastructure;

/// <summary>
/// Loads key=value pairs from a .env file into the ASP.NET Core configuration system.
/// Supports the double-underscore convention (Key__SubKey) for hierarchical bindings,
/// so Mcp__OAuth__Schemes__EntraId__TenantId maps to Mcp:OAuth:Schemes:EntraId:TenantId.
///
/// Searches for the .env file starting from <paramref name="directory"/> and walking up
/// the directory tree, so it works regardless of whether dotnet run is invoked from the
/// project folder or the repo root.
/// </summary>
public static class EnvFileExtensions
{
    /// <summary>
    /// Adds a .env-style file to the configuration builder, searching upward
    /// from the provided directory (defaults to the current directory).
    /// </summary>
    public static IConfigurationBuilder AddEnvFile(
        this IConfigurationBuilder builder,
        string directory = ".",
        bool optional = true)
    {
        var envPath = FindEnvFile(Path.GetFullPath(directory));

        if (envPath is null)
        {
            if (!optional)
                throw new FileNotFoundException(
                    ".env file not found. Searched from current directory upward.");
            return builder;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(envPath))
        {
            var trimmed = line.Trim();

            // Skip blank lines and comments
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var eq = trimmed.IndexOf('=');
            if (eq < 0)
                continue;

            var key = trimmed[..eq].Trim();
            var value = trimmed[(eq + 1)..].Trim();

            // Remove optional surrounding quotes
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Convert double-underscore to colon for hierarchical binding
            // (the same convention ASP.NET Core uses for environment variables)
            key = key.Replace("__", ":");

            data[key] = value;
        }

        builder.AddInMemoryCollection(data);
        return builder;
    }

    /// <summary>
    /// Walk up from <paramref name="startDir"/> looking for a .env file.
    /// Returns the full path of the first .env found, or null.
    /// </summary>
    private static string? FindEnvFile(string startDir)
    {
        var dir = startDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        while (true)
        {
            var candidate = Path.Combine(dir, ".env");
            if (File.Exists(candidate))
                return candidate;

            var parent = Path.GetDirectoryName(dir);
            if (parent is null || parent == dir)
                break;

            dir = parent;
        }

        return null;
    }
}
