using System;
using System.Collections.Generic;
using System.IO;

namespace EulerSSTools;

// Busca nombres conocidos en ubicaciones locales, sin acceder a red ni ejecutar archivos.
internal static class ToolDiscovery
{
    private static readonly Dictionary<string, string[]> ExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["journaltrace"] = ["JournalTrace.exe"],
        ["systeminformer"] = ["SystemInformer.exe", "ProcessHacker.exe"],
        ["winprefetch"] = ["WinPrefetchView.exe"],
        ["everything"] = ["Everything.exe"],
        ["lastactivity"] = ["LastActivityView.exe"],
        ["regscanner"] = ["RegScanner.exe"],
        ["cachedprograms"] = ["CachedProgramsList.exe"],
        ["meownovo"] = ["MeowNovowareFucker.exe"],
        ["meowdoom"] = ["MeowDoomsdayFucker.exe"],
        ["meowimports"] = ["MeowImportsChecker.exe"]
    };

    internal static string? Find(string id)
    {
        if (!ExeNames.TryGetValue(id, out var names)) return null;
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new[]
        {
            Path.Combine(user, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EulerSSTools", "Tools"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SystemInformer"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Everything")
        };

        foreach (var root in roots)
        {
            var found = FindInTree(root, names, maxDepth: 2);
            if (found is not null) return found;
        }
        return null;
    }

    private static string? FindInTree(string root, string[] names, int maxDepth)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;
        var work = new Stack<(string Folder, int Depth)>();
        work.Push((root, 0));
        while (work.Count > 0)
        {
            var (folder, depth) = work.Pop();
            foreach (string name in names)
            {
                try
                {
                    string candidate = Path.Combine(folder, name);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (Exception) { /* Ruta inaccesible; continuar. */ }
            }
            if (depth >= maxDepth) continue;
            try
            {
                foreach (var subfolder in Directory.EnumerateDirectories(folder))
                {
                    // No atravesar junctions/enlaces simbólicos hacia otras ubicaciones.
                    try
                    {
                        if ((File.GetAttributes(subfolder) & FileAttributes.ReparsePoint) == 0)
                            work.Push((subfolder, depth + 1));
                    }
                    catch (Exception) { /* Ignorar carpetas no legibles. */ }
                }
            }
            catch (Exception) { /* Ignorar carpetas no legibles. */ }
        }
        return null;
    }
}
