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
        var saveFolder    = GameSettings.GetSaveFolder(gameName);
        var projectFolder = Path.Combine(saveFolder, "Project");
        
        if (Directory.Exists(projectFolder)) {
            AnsiConsole.WriteLine($"Custom files found for {gameName}, copying over...");
            
            var targetFolder = settings.ExtractData.GetProjectPath();
            await Utility.CopyFilesRecursivelyPretty(projectFolder, targetFolder);
        }
        
        // copy from game settings
        var gameSettings = settings.GameSettings;
        projectFolder    = settings.ExtractData.GetProjectPath();
        var assetsPath   = Path.Combine(projectFolder, "Assets");
        foreach (var (from, to) in gameSettings.FileCopying.FilePaths ?? []) {
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
            
            File.Copy(fromPath, toPath);
        }
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
    }
    
    /// <summary>
    /// Attempts to convert any .txt files into another file type.
    /// </summary>
    public static void ParseTextFiles(ExtractData extractData) {
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
    
//     public static void FixMissingGuids(ToolSettings settings) {
//         var metaGuids = settings.GameSettings.FileOverrides.MetaGuids;
//         if (metaGuids == null) return;
//         if (metaGuids.Length == 0) return;
        
//         var projectPath = settings.ExtractData.Config.ProjectRootPath;
//         foreach (var (path, guidPath, line) in metaGuids) {
//             var filePath = Path.Combine(projectPath, path);
//             if (!File.Exists(filePath)) {
//                 throw new FileNotFoundException(filePath);
//             }
            
//             var metaPath = filePath + ".meta";
            
//             var extension = Path.GetExtension(path);
//             var metaContents = extension switch {
//                 ".shader" => @"fileFormatVersion: 2
// guid: #_GUID_
// timeCreated: 1741118198
// licenseType: Free
// ShaderImporter:
//   externalObjects: {}
//   defaultTextures: []
//   nonModifiableTextures: []
//   preprocessorOverride: 0
//   userData:
//   assetBundleName:
//   assetBundleVariant:
// ",
//                 _ => string.Empty,
//             };
            
//             if (string.IsNullOrEmpty(metaContents)) continue;
            
//             var guidFilePath     = Path.Combine(projectPath, guidPath);
//             if (!File.Exists(guidFilePath)) {
//                 throw new FileNotFoundException(guidFilePath);
//             }
            
//             var guidPathContents = File.ReadAllLines(guidFilePath);
//             var realLine         = line - 1;
//             if (realLine <= 0 || guidPathContents.Length < realLine) {
//                 throw new Exception($"Invalid line at {line} for guidPath: {guidFilePath}");
//             }
            
//             var textLine  = guidPathContents[realLine];
//             var guidMatch = UnityAssetTypes.GuidPattern.Match(textLine);
//             if (guidMatch == null || !guidMatch.Success) {
//                 throw new Exception($"No guid on line {line} for guidPath: {guidFilePath}");
//             }
            
//             var guid = guidMatch.Groups["guid"].Value;
//             metaContents = metaContents.Replace("#_GUID_", guid);
//             File.WriteAllText(metaPath, metaContents);
//         }
//     }
}
