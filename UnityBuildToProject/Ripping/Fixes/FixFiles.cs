using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Nomnom;

public static class FixFiles {
    /// <summary>
    /// Copies over the project structure from <c>/settings/[game name]/Project</c>
    /// if available.
    /// </summary>
    public static async Task CopyOverCustomFiles(ToolSettings settings) {
        var gameName      = settings.GetGameName();
        
        // copy from settings folder
        var projectFolder = GetSettingsProjectFolder(settings);
        
        if (Directory.Exists(projectFolder)) {
            AnsiConsole.WriteLine($"Custom files found for {gameName}, copying over...");
            
            var targetFolder = settings.ExtractData.GetProjectPath();
            await Utility.CopyFilesRecursivelyPretty(projectFolder, targetFolder);
        }
        
        // copy from game settings
        var gameSettings = settings.GameSettings;
        projectFolder    = settings.ExtractData.GetProjectPath();
        var assetsPath   = Path.Combine(projectFolder, "Assets");
        foreach (var (from, to) in gameSettings.Files.CopyFilePaths ?? []) {
            if (string.IsNullOrEmpty(from)) continue;
            
            var fromPath = Utility.ReplacePathModifier(settings, from);
            var toPath   = string.IsNullOrEmpty(to) 
                ? Path.Combine("Plugins", Path.GetFileName(from))
                : to;
            toPath       = Path.Combine(assetsPath, toPath);
            
            AnsiConsole.WriteLine($"Copying {fromPath, 4} to {Utility.ClampPathFolders(toPath, 4)}");
            
            if (!File.Exists(fromPath)) {
                throw new FileNotFoundException(fromPath);
            }
            
            var toPathFolder = Path.GetDirectoryName(toPath)!;
            Directory.CreateDirectory(toPathFolder);
            
            File.Copy(fromPath, toPath, true);
        }
    }
    
    public static string GetSettingsProjectFolder(ToolSettings settings) {
        var gameName      = settings.GetGameName();
        
        // copy from settings folder
        var saveFolder    = GameSettings.GetSaveFolder(gameName);
        var projectFolder = Path.Combine(saveFolder, "Project");
        
        return projectFolder;
    }
    
    /// <summary>
    /// Imports any .unitypackage from <c>/settings/[game name]/UnityPackages</c>
    /// if available.
    /// </summary>
    public static async Task ImportCustomUnityPackages(ToolSettings settings) {
        var gameName       = settings.GetGameName();
        var saveFolder     = GameSettings.GetSaveFolder(gameName);
        var packagesFolder = Path.Combine(saveFolder, "UnityPackages");
        
        if (!Directory.Exists(packagesFolder)) return;
        
        AnsiConsole.WriteLine($"Additional unity packages found for {gameName}, importing...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var files       = Directory.GetFiles(packagesFolder, "*.unitypackage", SearchOption.TopDirectoryOnly);
        foreach (var file in files) {
            var unityPath = settings.GetUnityPath();
            var name      = Path.GetFileName(file);
            await UnityCLI.OpenProjectHidden($"Importing {name}", unityPath, true, projectPath,
                $"-importPackage \"{file}\""
            );
        }
        
        foreach (var file in settings.GameSettings.Packages.ImportUnityPackages ?? []) {
            var unityPath = settings.GetUnityPath();
            var name      = Path.GetFileName(file.Path);
            var path      = Path.GetFullPath(
                Path.Combine(projectPath, file.Path)
            );
            
            if (!File.Exists(path)) {
                throw new FileNotFoundException(path);
            }
            
            await UnityCLI.OpenProjectHidden($"Importing {name}", unityPath, true, projectPath,
                $"-importPackage \"{path}\""
            );
        }
    }
    
    public static async Task ImportUnityPackage(ToolSettings settings, string path) {
        var name = Path.GetFileName(path);
        if (!File.Exists(path)) {
            throw new FileNotFoundException(path);
        }
        
        var unityPath = settings.GetUnityPath();
        await UnityCLI.OpenProjectHidden($"Importing {name}", unityPath, true, settings.ExtractData.GetProjectPath(),
            $"-importPackage \"{path}\""
        );
    }
    
    /// <summary>
    /// Attempts to convert any .txt files into another file type.
    /// </summary>
    public static void ParseTextFiles(ExtractData extractData) {
        Console.WriteLine($"Parsing text files...");
        
        var projectPath = extractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var txtFiles    = Directory.GetFiles(assetsPath, "*.txt", SearchOption.AllDirectories);
        
        foreach (var file in txtFiles) {
            Console.WriteLine($"Decoding {Utility.ClampPathFolders(file, 6)}");

            var lines = File.ReadAllLines(file);
            if (lines.Length == 0) continue;

            // todo: make this better
            // check for a .txt -> .csv
            var line = lines[0];
            if (!line.Contains(',')) continue;
            
            var csvPath = Path.ChangeExtension(file, ".csv");
            File.Move(file, csvPath, true);

            var metaPath = file + ".meta";
            if (File.Exists(metaPath)) {
                var metaCsvPath = csvPath + ".meta";
                File.Move(metaPath, metaCsvPath, true);
            }
        }
    }
    
    /// <summary>
    /// Fixes ripped shader contents for things like wrongly-typed values.
    /// </summary>
    public static void FixShaders(ExtractData extractData) {
        Console.WriteLine($"Fixing shaders...");
        
        var projectPath = extractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var shaderFiles = Directory.GetFiles(assetsPath, "*.shader", SearchOption.AllDirectories);
        
        // fix fog
        var fogPattern = @"(?s)Fog\s*\{\s*Mode\s*(\d)\s*\}";
        foreach (var file in shaderFiles) {
            Console.WriteLine($"Fixing {Utility.ClampPathFolders(file, 6)}");

            var text   = File.ReadAllText(file);
            var result = Regex.Replace(text, fogPattern, x => {
                var mode = x.Groups[1].Value == "0" ? "Off" : "On";
                return $"Fog {{ Mode {mode} }}";
            });
            
            File.WriteAllText(file, result);
        }
    }
    
    /// <summary>
    /// Moves filtered assets into sub-folders to maintain their
    /// non-underscore names. Useful for assets that require name lookup.
    /// </summary>
    public static void FixDuplicateAssets(ToolSettings settings) {
        Console.WriteLine($"Fixing duplicate assets...");
        
        var toProcess = new string[] {
            "AnimationClip"
        };
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        
        var underscorePattern = Utility.GetFileUnderscoreRegex();
        foreach (var path in toProcess) {
            AnsiConsole.WriteLine($"Fixing duplicate assets in {path}");
            
            var folderPath = Path.Combine(assetsPath, path);
            if (!Directory.Exists(folderPath)) continue;
            
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files) {
                // if the file ends in _0 then we de-duplicate it
                var match = underscorePattern.Match(file);

                if (match == null || !match.Success) {
                    continue;
                }
                
                var dirName = Path.GetDirectoryName(file)!;
                var oldName = Path.GetFileNameWithoutExtension(file);
                var newName = Path.GetFileName(match.Groups["base"].Value);
                var newFolder = Path.Combine(
                    dirName,
                    Path.GetFileNameWithoutExtension(oldName)
                );

                Directory.CreateDirectory(newFolder);

                var extension = Path.GetExtension(file);
                if (Path.HasExtension(oldName)) {
                    extension = $"{Path.GetExtension(oldName)}{extension}";
                }
                var newPath = Path.Combine(newFolder, newName + extension);
                
                File.Move(file, newPath);

                AnsiConsole.WriteLine($" - fixed {Utility.ClampPathFolders(file, 6)}");
            }
        }
    }
    
    public static void FixAssetNames(ToolSettings settings) {
        Console.WriteLine($"Fixing asset names...");
        
        var toProcess = new string[] {
            "MonoBehaviour",
            "AnimationClip",
            "Material"
        };
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        
        var sb = new StringBuilder();
        foreach (var path in toProcess) {
            AnsiConsole.WriteLine($"Fixing asset names in {path}");
            
            var folderPath = Path.Combine(assetsPath, path);
            if (!Directory.Exists(folderPath)) continue;
            
            var files = Directory.GetFiles(folderPath, "*.asset", SearchOption.AllDirectories);
            foreach (var file in files) {
                AnsiConsole.WriteLine($" - checking {Utility.ClampPathFolders(file, 6)}");
                
                sb.Clear();
                
                var text = File.ReadAllText(file);
                var name = Path.GetFileNameWithoutExtension(file);
                using (var reader = new StreamReader(file)) {
                    while (reader.Peek() >= 0) {
                        // read lines
                        var line = reader.ReadLine();
                        if (line == null) continue;
                        
                        if (line.TrimStart().StartsWith("m_Name:")) {
                            var leading = Utility.GetLeadingWhitespace(line);
                            sb.AppendLine($"{leading}m_Name: {name}");
                            break;
                        }
                        
                        sb.AppendLine(line);
                    }
                    
                    // flush lines
                    while (reader.Peek() >= 0) {
                        var line = reader.ReadLine();
                        if (line == null) continue;
                        
                        sb.AppendLine(line);
                    }
                }
                
                File.WriteAllText(file, sb.ToString());
                
                AnsiConsole.WriteLine($" - fixed {Utility.ClampPathFolders(file, 6)}");
            }
        }
    }
    
    public static void CleanupDeadMetaFiles(ToolSettings settings) {
        Console.WriteLine($"Cleaning dead meta files...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var files       = Directory.GetFiles(assetsPath, "*.meta", SearchOption.AllDirectories);
        
        foreach (var file in files) {
            var path = file[..^".meta".Length];
            if (!Directory.Exists(path) && !File.Exists(path)) {
                File.Delete(file);
            }
        }
    }
    
    public static void FixAmbiguousUsages(ToolSettings settings, (string[] usages, string addition)[] allUsages) {
        Console.WriteLine($"Fixing ambiguous usages...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var scriptsPath = Path.Combine(assetsPath, "Scripts");
        
        var files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files) {
            var text = File.ReadAllText(file);
            foreach (var (usages, addition) in allUsages) {
                if (usages.All(x => text.Contains($"using {x};"))) {
                    var first = $"using {usages[0]};";
                    text = text.Replace(first, $"{first}\n{addition}");
                    File.WriteAllText(file, text);
                }
            }
        }
    }
    
    public static void RemovePrivateDetails(ToolSettings settings) {
        Console.WriteLine($"Removing private details...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var scriptsPath = Path.Combine(assetsPath, "Scripts");
        
        var files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        var lines = new List<string>(1024);
        foreach (var file in files) {
            Console.WriteLine($"Checking {file}");
            
            lines.Clear();
            lines.AddRange(File.ReadAllLines(file));
            
            var startCount = lines.Count;
            for (int i = 0; i < lines.Count; i++) {
                var line = lines[i];
                if (line.Contains("global::") && line.Contains("PrivateImplementationDetails")) {
                    // remove line
                    lines.RemoveAt(i--);
                }
            }
            
            if (startCount != lines.Count) {
                File.WriteAllLines(file, lines);
            }
        }
    }

    // todo: make better version to handle complex cases
    public static void FixCheckedGetHashCodes(ToolSettings settings) {
        Console.WriteLine($"Fixing GetHashCodes...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var scriptsPath = Path.Combine(assetsPath, "Scripts");
        
        var files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        var lines = new List<string>(1024);
        foreach (var file in files) {
            Console.WriteLine($"Checking {file}");
            
            lines.Clear();
            lines.AddRange(File.ReadAllLines(file));
            
            var ident      = 0;
            var foundStart = false;
            var foundEnd   = false;
            
            for (int i = 0; i < lines.Count; i++) {
                var line = lines[i];
                if (line.Contains("public override int GetHashCode()")) {
                    lines.Insert(i + 2, "unchecked {");
                    ident++;
                    i += 2;
                    
                    Console.WriteLine(" - found GetHashCode");
                    foundStart = true;
                    continue;
                }
                
                if (foundStart) {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) {
                        if (trimmed[0] == '{') {
                            ident++;
                        } else if (trimmed[0] == '}') {
                            ident--;
                        }
                    }
                    
                    if (ident == 0) {
                        lines.Insert(i, "}");
                        foundEnd = true;
                        break;
                    }
                }
            }
            
            if (foundStart && foundEnd) {
                Console.WriteLine($" - fixed");
                File.WriteAllLines(file, lines);
            }
        }
    }
    
    public static void ReplaceFileContents(ToolSettings settings) {
        Console.WriteLine($"Replacing file contents...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var files       = settings.GameSettings.Files.ReplaceContents ?? [];
        var groups      = files.GroupBy(x => x.Path);
        
        foreach (var group in groups) {
            var path = Path.Combine(projectPath, group.Key);
            if (!File.Exists(path)) {
                throw new FileNotFoundException(path);
            }
            
            Console.WriteLine($"Fixing {path}");
            var text = File.ReadAllText(path);
            foreach (var replacement in group) {
                var regex = new Regex(replacement.Find);
                text = regex.Replace(text, replacement.Replacement);
            }
            File.WriteAllText(path, text);
        }
    }
}
