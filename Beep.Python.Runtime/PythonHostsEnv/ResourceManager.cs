using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Manages embedded resources in the Runtime assembly
/// </summary>
public static class ResourceManager
{
    private static readonly Assembly RuntimeAssembly = typeof(ResourceManager).Assembly;

    /// <summary>
    /// Get embedded resource as string
    /// </summary>
    /// <param name="resourcePath">Resource path (e.g., "Configuration/config.json")</param>
    /// <returns>Resource content as string</returns>
    public static async Task<string?> GetEmbeddedResourceAsString(string resourcePath)
    {
        var resourceName = $"Beep.Python.Runtime.{resourcePath.Replace("/", ".").Replace("\\", ".")}";
        
        using var stream = RuntimeAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Get embedded resource as stream
    /// </summary>
    /// <param name="resourcePath">Resource path (e.g., "Python/script.py")</param>
    /// <returns>Resource stream</returns>
    public static Stream? GetEmbeddedResourceAsStream(string resourcePath)
    {
        var resourceName = $"Beep.Python.Runtime.{resourcePath.Replace("/", ".").Replace("\\", ".")}";
        return RuntimeAssembly.GetManifestResourceStream(resourceName);
    }

    /// <summary>
    /// Extract embedded resource to file
    /// </summary>
    /// <param name="resourcePath">Resource path</param>
    /// <param name="targetPath">Target file path</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> ExtractEmbeddedResource(string resourcePath, string targetPath)
    {
        try
        {
            using var stream = GetEmbeddedResourceAsStream(resourcePath);
            if (stream == null)
                return false;

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            using var fileStream = File.Create(targetPath);
            await stream.CopyToAsync(fileStream);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract all Python scripts to a directory
    /// </summary>
    /// <param name="targetDirectory">Target directory</param>
    /// <returns>True if successful</returns>
    public static async Task<bool> ExtractPythonScripts(string targetDirectory)
    {
        try
        {
            Directory.CreateDirectory(targetDirectory);

            var pythonFiles = new[]
            {
                "requirements.txt",
                "script.py"
            };

            foreach (var file in pythonFiles)
            {
                var resourcePath = $"Python/{file}";
                var targetPath = Path.Combine(targetDirectory, file);
                
                if (!await ExtractEmbeddedResource(resourcePath, targetPath))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get list of all embedded resource names
    /// </summary>
    /// <returns>List of resource names</returns>
    public static string[] GetEmbeddedResourceNames()
    {
        return RuntimeAssembly.GetManifestResourceNames();
    }

    /// <summary>
    /// Check if embedded resource exists
    /// </summary>
    /// <param name="resourcePath">Resource path</param>
    /// <returns>True if resource exists</returns>
    public static bool ResourceExists(string resourcePath)
    {
        var resourceName = $"Beep.Python.Runtime.{resourcePath.Replace("/", ".").Replace("\\", ".")}";
        return RuntimeAssembly.GetManifestResourceNames().Contains(resourceName);
    }
}
