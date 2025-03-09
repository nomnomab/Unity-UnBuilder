using System.Reflection;
using Spectre.Console;

namespace Nomnom;

public static class Paths {
    public static string CurrentDirectory => Directory.GetCurrentDirectory();
    
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
    
    public static async Task DeleteDirectory(string path, string[]? excludeFolders = null) {
        if (!Directory.Exists(path)) {
            throw new DirectoryNotFoundException(path);
        }
        
        AnsiConsole.MarkupLine("[red]Deleting[/] files. This may take a while!");
        foreach (var exclude in excludeFolders ?? []) {
            Console.WriteLine($" - excluding: {exclude}");
        }
        
        var roots = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
            .Where(x => excludeFolders == null || !excludeFolders.Contains(Path.GetFileName(x)));
        var tasks = new List<Task>();
        foreach (var root in roots) {
            var rootPath = Path.GetFullPath(root);
            tasks.Add(Task.Run(() => {
                var shorter = Utility.ClampPathFolders(rootPath, 6);
                var dirInfo = new DirectoryInfo(rootPath);
                var files   = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                
                AnsiConsole.MarkupLine($"[red]Deleting[/] {files.Length} file(s) for {shorter}...");
                files.AsParallel()
                    .ForAll(x => x.Delete());
                
                Directory.Delete(rootPath, true);
                AnsiConsole.MarkupLine($"[green]Finished[/] with {shorter}");
            }));
        }
        
        AnsiConsole.MarkupLine($"Executing across {tasks.Count} tasks(s)...");
        
        await Task.WhenAll(tasks);
        
        // final deletion pass to make sure the entire path is gone
        if (excludeFolders == null) {
            Directory.Delete(path, true);
        }
    }
}
