using System.Reflection;
using Spectre.Console;

namespace Nomnom;

public static class Paths {
    public static string ExeFolder {
        get {
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException(exePath);
            }
            
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    exePath,
                    ".."
                )
            );
            
            return outputPath;
        }
    }
    
    public static string ResourcesFolder {
        get {
            var path = Path.Combine(ExeFolder, "Resources");
            EnsureDirectory(path);
            return path;
        }
    }
    
    public static string LogsFolder {
        get {
            var path = Path.Combine(ExeFolder, "logs");
            EnsureDirectory(path);
            return path;
        }
    }
    
    public static string LibFolder {
        get {
            var path = Path.Combine(ExeFolder, "lib");
            EnsureDirectory(path);
            return path;
        }
    }
    
    private static void EnsureDirectory(string path) {
        Directory.CreateDirectory(path);
    }
    
    public static async Task DeleteDirectory(string path) {
        if (!Directory.Exists(path)) {
            return;
        }
        
        AnsiConsole.MarkupLine("[red]Deleting[/] previous project. This will take a while!");
        
        var roots = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        var tasks = new List<Task>();
        foreach (var root in roots) {
            var rootPath = root;
            tasks.Add(Task.Run(() => {
                Directory.Delete(rootPath, true);
                AnsiConsole.MarkupLine($" - {Utility.ClampPathFolders(rootPath, 4)} is done");
            }));
        }
        
        AnsiConsole.MarkupLine($"Executing across {tasks.Count} tasks(s)...");
        
        await Task.WhenAll(tasks);
        
        // final deletion pass to make sure the entire path is gone
        Directory.Delete(path, true);
    }
}
