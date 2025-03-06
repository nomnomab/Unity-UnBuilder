using System.Reflection;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Nomnom;

public static partial class Utility {
    private static readonly string? CallingAssemblyName = Assembly.GetCallingAssembly()?.GetName().Name;
    public static bool IsRunningTests = CallingAssemblyName == "UnityBuildToProject.Tests";
    
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
    
    public static async Task CopyAssets(string sourcePath, string targetPath, HashSet<string> fileBlacklist) {
        AnsiConsole.MarkupLine($"Copying assets from \"{sourcePath}\" to \"{targetPath}\"...");
        
        foreach (var file in fileBlacklist) {
            Console.WriteLine($" - without: {file}");
        }
        
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
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                    .Where(x => !Path.GetFileName(x).StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes"))
                    .Where(x => !fileBlacklist.Contains(x));
                
                foreach (var file in files) {
                    AnsiConsole.MarkupLine($"[grey]Copying[/] \"{ClampPathFolders(file, 4)}\" to \"{ClampPathFolders(targetPath, 4)}\"");
                    
                    // generic scripts tend to have this
                    if (file.Contains('`')) continue;
                    
                    // older assets generate files with just dashes
                    var noDash = file.Replace("-", string.Empty);
                    if (string.IsNullOrEmpty(noDash)) {
                        continue;
                    }
                    
                    // if (file.Contains("-PrivateImplementationDetails-")) {
                    //     continue;
                    // }
                    
                    var to      = file.Replace(sourcePath, targetPath);
                    var folder  = Path.GetDirectoryName(to);
                    // var isValid = FileNonSuffixExists(file) is 0 or 2;
                    
                    var extension = Path.GetExtension(to);
                    if (extension == ".new") {
                        var realTo = to[..^".new".Length];
                        if (fileBlacklist.Contains(realTo)) continue;
                        
                        extension = Path.GetExtension(realTo);
                        if (extension == ".meta") {
                            var normalName = Path.GetFileNameWithoutExtension(file);
                            normalName = Path.GetFileNameWithoutExtension(normalName);
                            
                            // skip meta files that have no assets
                            var assetPath = Path.Combine(
                                Path.GetDirectoryName(file)!,
                                normalName
                            );
                            
                            var assetNewPath = Path.Combine(
                                Path.GetDirectoryName(file)!,
                                normalName + ".new"
                            );
                            
                            Console.WriteLine($"new assetPath: {assetPath}\nassetNewPath: {assetNewPath}");
                            
                            if (!File.Exists(assetPath) && !File.Exists(assetNewPath)) {
                                continue;
                            }
                        }
                        
                        Directory.CreateDirectory(folder!);
                        File.Copy(file, realTo, true);
                        
                        // clean up .new files
                        File.Delete(file);
                    } else {
                        if (extension == ".meta") {
                            // skip meta files that have no assets
                            var assetPath = Path.Combine(
                                Path.GetDirectoryName(file)!,
                                Path.GetFileNameWithoutExtension(file)
                            );
                            
                            var assetNewPath = Path.Combine(
                                Path.GetDirectoryName(file)!,
                                Path.GetFileNameWithoutExtension(file) + ".new"
                            );
                            
                            Console.WriteLine($"not new assetPath: {assetPath}\nassetNewPath: {assetNewPath}");
                            
                            if (!File.Exists(assetPath) && !File.Exists(assetNewPath)) {
                                continue;
                            }
                        }
                        
                        Directory.CreateDirectory(folder!);
                        File.Copy(file, to, true);
                    }
                    
                    await Task.Yield();
                }
            });
        
        AnsiConsole.MarkupLine("[green]Done[/] copying!");
    }
    
    // 0: wrong file format
    // 1: file doesn't exist
    // 2: file has owner
    // public static int FileNonSuffixExists(string filePath) {
    //     if (!filePath.Contains(".shader")) {
    //         return 0;
    //     }
        
    //     var fileName = Path.GetFileName(filePath);
    //     var directory = Path.GetDirectoryName(filePath)!;

    //     // Regex to detect a suffix file.
    //     // This pattern captures:
    //     //  - a base name (non-greedy), followed by
    //     //  - an underscore and one or more digits, then
    //     //  - the file extension.
    //     var regex = GetFileUnderscoreRegex();
    //     var match = regex.Match(fileName);

    //     if (match.Success) {
    //         // Reconstruct the non-suffix file name.
    //         var baseName = match.Groups["base"].Value;
    //         var extension = match.Groups[1].Value;
    //         var nonSuffixFile = Path.Combine(directory, baseName + extension);

    //         // Check if the corresponding non-suffix file exists.
    //         if (!File.Exists(nonSuffixFile)) {
    //             return 1;
    //         }
            
    //         return 2;
    //     }
        
    //     return 0;
    // }
    
    public static string CopyOverScript(string projectPath, string localPath, Func<string, string>? updateText = null) {
        var folder = GetEditorScriptFolder(projectPath);
        Directory.CreateDirectory(folder);
        
        var name         = Path.GetFileNameWithoutExtension(localPath);
        var scriptPath   = Path.Combine(folder, $"{name}.cs");
        var path         = Path.Combine(Paths.ResourcesFolder, $"{name}.cs.txt");
        using var stream = new StreamReader(path);
        var contents     = stream.ReadToEnd();
        
        if (updateText != null) {
            contents = updateText(contents);
        }
        
        File.WriteAllText(scriptPath, contents);
        return scriptPath;
    }
    
    public static string GetEditorScriptFolder(string projectPath) {
        return Path.Combine(projectPath, "Assets", "_CustomEditor", "Editor");
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

    [GeneratedRegex(@"^(?<base>.+?)(?:_\d+)((?:\.[^.]+)+)$", RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex GetFileUnderscoreRegex();
}

public record ProfileDuration {
    public DateTime Start = DateTime.Now;
    public DateTime Origin = DateTime.Now;
    
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
        if (Timestamps.Count == 0) return;
        
        var maxLabelLength = Timestamps.Max(x => x.Label.Length) + 1;
        
        foreach (var (label, duration) in Timestamps) {
            AnsiConsole.MarkupLine($"[green]{label.PadRight(maxLabelLength)}[/]: {duration}");
        }
        
        AnsiConsole.WriteLine();
        
        var total = DateTime.Now - Origin;
        AnsiConsole.MarkupLine($"[green]Total[/]: {total}");
    }
}

public record ProfileDurationTimestamp(string Label, TimeSpan Duration);
