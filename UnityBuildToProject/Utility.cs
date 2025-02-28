using Spectre.Console;

namespace Nomnom;

public static class Utility {
    public static void CopyFilesRecursively(string sourcePath, string targetPath) {
        // create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        // copy all the files & replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories)) {
            var to = newPath.Replace(sourcePath, targetPath);
            var folder = Path.GetDirectoryName(to);
            Directory.CreateDirectory(folder!);
            File.Copy(newPath, to, true);
        }
    }
    
    public static async Task CopyFilesRecursivelyPretty(string sourcePath, string targetPath) {
        AnsiConsole.MarkupLine($"Copying \"{sourcePath}\" to \"{targetPath}\"...");
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .StartAsync("...", async ctx => {
                ctx.Status("Creating directories");
                
                // create all of the directories
                var directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
                foreach (var dirPath in directories) {
                    AnsiConsole.MarkupLine($"[grey]Copying[/] \"{dirPath}\" to \"\"{targetPath}");
                    
                    var dir = dirPath.Replace(sourcePath, targetPath);
                    Directory.CreateDirectory(dir);
                    
                    await Task.Yield();
                }

                ctx.Status("Creating Files");

                // copy all the files & replaces any files with the same name
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                foreach (var newPath in files) {
                    AnsiConsole.MarkupLine($"[grey]Copying[/] \"{newPath}\" to \"{targetPath}\"");
                    
                    var to = newPath.Replace(sourcePath, targetPath);
                    var folder = Path.GetDirectoryName(to);
                    
                    Directory.CreateDirectory(folder!);
                    File.Copy(newPath, to, true);
                    
                    await Task.Yield();
                }
            });
        
        AnsiConsole.MarkupLine("[green]Done[/] copying!");
    }
    
    public static async Task CopyAssets(string sourcePath, string targetPath) {
        AnsiConsole.MarkupLine($"Copying assets from \"{sourcePath}\" to \"{targetPath}\"...");
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .StartAsync("...", async ctx => {
                ctx.Status("Creating directories");
                
                // create all of the directories
                var directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
                foreach (var dirPath in directories) {
                    AnsiConsole.MarkupLine($"[grey]Copying[/] \"{dirPath}\" to \"\"{targetPath}");
                    
                    var dir = dirPath.Replace(sourcePath, targetPath);
                    Directory.CreateDirectory(dir);
                    
                    await Task.Yield();
                }

                ctx.Status("Creating Files");

                // copy all the files & replaces any files with the same name
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files) {
                    AnsiConsole.MarkupLine($"[grey]Copying[/] \"{file}\" to \"{targetPath}\"");
                    
                    var to = file.Replace(sourcePath, targetPath);
                    var folder = Path.GetDirectoryName(to);
                    
                    if (Path.GetExtension(to) == ".new") {
                        var realTo = to[..^".new".Length];
                        Directory.CreateDirectory(folder!);
                        File.Copy(file, realTo, true);
                    } else {
                        Directory.CreateDirectory(folder!);
                        File.Copy(file, to, true);
                    }
                    
                    
                    await Task.Yield();
                }
            });
        
        AnsiConsole.MarkupLine("[green]Done[/] copying!");
    }
    
    public static void CopyOverScript(string projectPath, string name, Func<string, string>? updateText = null) {
        var folder = GetEditorScriptFolder(projectPath);
        Directory.CreateDirectory(folder);
        
        var scriptPath   = Path.Combine(folder, $"{name}.cs");
        var path         = Path.Combine(Program.OutputFolder, "Resources", $"{name}.cs.txt");
        using var stream = new StreamReader(path);
        var contents     = stream.ReadToEnd();
        
        if (updateText != null) {
            contents = updateText(contents);
        }
        
        File.WriteAllText(scriptPath, contents);
    }
    
    public static string GetEditorScriptFolder(string projectPath) {
        return Path.Combine(projectPath, "Assets", "Editor");
    }
    
    public static string ClampPathFolders(string path, int maxFolders) {
        var index = -1;
        var count = 0;
        for (int i = path.Length - 1; i >= 0 ; i--) {
            if (path[i] == Path.DirectorySeparatorChar || path[i] == '/' || path[i] == '\\') {
                index = i;
                count++;
                
                if (count == maxFolders) break;
            }
        }
        
        if (index == -1) {
            return path;
        }
        
        return path[(index + 1)..];
    }
}

public record ProfileDuration {
    public DateTime Start = DateTime.Now;
    
    public List<ProfileDurationTimestamp> Timestamps = [];
    
    public void New() {
        Start = DateTime.Now;
    }
    
    public void Record(string label) {
        var time = DateTime.Now;
        var diff = time - Start;
        
        Timestamps.Add(new ProfileDurationTimestamp(label, diff));
        New();
    }
    
    public void PrintTimestamps() {
        var maxLabelLength = Timestamps.Max(x => x.Label.Length) + 1;
        
        foreach (var (label, duration) in Timestamps) {
            AnsiConsole.MarkupLine($"[green]{label.PadRight(maxLabelLength)}[/]: {duration.Minutes}m {duration.Seconds}s");
        }
    }
}

public record ProfileDurationTimestamp(string Label, TimeSpan Duration);
