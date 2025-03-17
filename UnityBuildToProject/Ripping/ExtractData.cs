using AssetRipper.Export.Modules.Textures;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Configuration;
using AssetRipper.Processing;
using AssetRipper.Processing.Configuration;
using Spectre.Console;
using Tomlet.Attributes;

namespace Nomnom;

public record ExtractData {
    // private const string ProjectName = "Out";
    
    public required string GameName;
    public required string OutputFolder;
    public required GameData GameData;
    public required LibraryConfiguration Config;
    
    public string GetProjectPath() {
        // return Path.Combine(Config.ProjectRootPath, "..", $"{GameName}_{ProjectName}");
        // return Path.Combine(Paths.CurrentDirectory, $"{GameName}_{ProjectName}");
        // return Path.Combine(OutputFolder);
        return Path.Combine(OutputFolder, GameName);
    }
    
    public async Task<string> CreateNewProject(ProgramArgs args) {
        var projectPath     = Config.ProjectRootPath;
        var newProjectPath  = GetProjectPath();
        
        // delete the old project across multiple tasks if possible
        if (Directory.Exists(newProjectPath)) {
            if (!args.SkipPackageAll) {
                await Paths.DeleteDirectory(newProjectPath);
            } else {
                await Paths.DeleteDirectory(newProjectPath, excludeFolders: [
                    "Packages",
                    "Library",
                    "ProjectSettings",
                    "UserSettings"
                ]);
            }
        }
        
        Directory.CreateDirectory(newProjectPath);
        Directory.CreateDirectory(Path.Combine(newProjectPath, "Assets"));
        
        if (!args.SkipPackageAll) {
            await Utility.CopyFilesRecursivelyPretty(
                Path.Combine(projectPath, "ProjectSettings"), 
                Path.Combine(newProjectPath, "ProjectSettings")
            );
        }
        
        return newProjectPath;
    }
    
    public IEnumerable<string> GetSharedDlls(string secondProject) {
        var exportProject = Path.Combine(Config.AuxiliaryFilesPath, "..");
        return GetSharedDlls(exportProject, secondProject);
    }
    
    public static IEnumerable<string> GetSharedDlls(string exportProject, string secondProject) {
        var firstPath  = Path.Combine(exportProject, "AuxiliaryFiles", "GameAssemblies");
        var secondPath = Path.Combine(secondProject, "Library", "ScriptAssemblies");
        
        var firstDlls  = Directory.GetFiles(firstPath, "*.dll")
            .Select(x => x[(firstPath.Length + 1)..]);
        var secondDlls = Directory.GetFiles(secondPath, "*.dll")
            .Select(x => x[(secondPath.Length + 1)..]);
        
        return firstDlls.Union(secondDlls);
    }
    
    public async Task CopyAssetsToProject(ToolSettings settings, string secondProject, HashSet<string> fileBlacklist, HashSet<string> folderBlacklist, GuidDatabase guidDb, bool testFoldersOnly) {
        var firstPath  = Config.AssetsPath;
        var secondPath = Path.Combine(secondProject, "Assets");
        
        // append to blacklist
        var scriptsFolder = Path.Combine(firstPath, "Scripts");
        
        // exclude folders that match a dll name
        foreach (var name in PackageAssociations.ExcludeNamesFromProject) {
            AnsiConsole.WriteLine($"Checking exclusion for \"{name}\"");
            foreach (var dir in Directory.GetDirectories(scriptsFolder, name, SearchOption.TopDirectoryOnly)) {
                folderBlacklist.Add(dir);
                fileBlacklist.Add(dir + ".meta");
                AnsiConsole.WriteLine($" - excluding: \"{dir}\"");
            }
        }
        
        // exclude folders that start with a prefix
        foreach (var name in PackageAssociations.ExcludePrefixesFromProject) {
            AnsiConsole.WriteLine($"Checking exclusion for prefix \"{name}\"");
            foreach (var dir in Directory.GetDirectories(scriptsFolder, $"{name}*", SearchOption.TopDirectoryOnly)) {
                folderBlacklist.Add(dir);
                fileBlacklist.Add(dir + ".meta");
                AnsiConsole.WriteLine($" - excluding: \"{dir}\"");
            }
        }
        
        folderBlacklist.Add(
            Path.Combine(firstPath, "ComputeShader")
        );
        
        foreach (var folder in folderBlacklist) {
            if (!Directory.Exists(folder)) continue;
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            foreach (var file in files) {
                var realFile = Path.GetFullPath(file);
                fileBlacklist.Add(realFile);
                fileBlacklist.Add(realFile + ".meta");
                AnsiConsole.WriteLine($" - excluding: \"{realFile}\"");
            }
        }
        
        // keep safe files around
        foreach (var safeFile in GetSafeExportPaths(settings)) {
            AnsiConsole.WriteLine($" - accepting \"{safeFile}\"");
            AnsiConsole.WriteLine($" - accepting \"{safeFile}.meta\"");
            
            fileBlacklist.Remove(safeFile);
            fileBlacklist.Remove(safeFile + ".meta");
        }
        
        AnsiConsole.WriteLine($"Copying project folders to \"{secondProject}\"");
        
        // copy over everything
        // filter out while debugging
        if (testFoldersOnly) {
            var folders = new string[] {
                "AnimationClip",
                "Avatar",
                "Cubemap",
                "Scripts",
                "MonoBehaviour",
                "Material",
                "Mesh",
                "Texture2D",
                "Sprite",
                "Shader",
                "Scenes",
                "Resources",
                "Plugins",
                // "ComputeShader",
                "Editor"
            };
            
            foreach (var folder in folders) {
                var folderPath = Path.Combine(firstPath, folder);
                if (!Directory.Exists(folderPath)) {
                    continue;
                }
                
                await Utility.CopyAssets(
                    Path.Combine(firstPath, folder), 
                    Path.Combine(secondPath, folder),
                    fileBlacklist
                );
                await Task.Delay(50);
            }
        } else {
            await Utility.CopyAssets(firstPath, secondPath, fileBlacklist);
        }
            
        AnsiConsole.MarkupLine("[green]Finished[/] copying folders to the temp project!");
    }
    
    public static IEnumerable<string> GetSafeExportPaths(ToolSettings settings) {
        var rootPath    = settings.ExtractData.Config.ProjectRootPath;
        return GetSafePaths(settings, rootPath);
    }
    
    public static IEnumerable<string> GetSafePaths(ToolSettings settings, string rootPath) {
        var projectPath = settings.GetSettingsProjectFolder();
        
        if (Directory.Exists(projectPath)) {
            var dirs = Directory.GetDirectories(projectPath, "*.*", SearchOption.AllDirectories);
            foreach (var dir in dirs) {
                var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files) {
                    var newPath = file.Replace(
                        projectPath,
                        rootPath
                    );
                    
                    yield return newPath;
                }
            }
            
            {
                var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files) {
                    var newPath = file.Replace(
                        projectPath,
                        rootPath
                    );
                    
                    yield return newPath;
                }
            }
        }
        
        // todo: update to new stuff
        // foreach (var file in settings.GameSettings.FileOverrides.ProjectPaths ?? []) {
        //     var path = Path.GetFullPath(
        //         Path.Combine(rootPath, file.Path)
        //     );
            
        //     if (Directory.Exists(path)) {
        //         var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        //         foreach (var file2 in files) {
        //             var newPath = file2.Replace(
        //                 projectPath,
        //                 rootPath
        //             );
                    
        //             yield return newPath;
        //         }
        //     } else {
        //         yield return path;
        //     }
        // }
    }
    
    public static void RemoveExistingPackageFoldersFromProjectScripts(ToolSettings settings, string projectPath) {
        AnsiConsole.MarkupLine($"[red]Deleting[/] existing package folders from \"{projectPath}\"");
        
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .Start("...", ctx => {
                var scriptsFolder = Path.Combine(projectPath, "Assets", "Scripts");
                var folders       = Directory.GetDirectories(scriptsFolder);
                var safePaths     = GetSafePaths(settings, projectPath).ToArray();
                
                foreach (var path in safePaths) {
                    AnsiConsole.WriteLine($"safe path: {path}");
                }
                
                foreach (var dir in folders) {
                    var name = Path.GetFileName(dir);
                    ctx.Status($"Checking {Utility.ClampPathFolders(dir, 4)}...");
                    
                    var shouldDelete = PackageAssociations.FindAssociationFromDll(name) != null;
                    if (!shouldDelete) {
                        foreach (var excl in PackageAssociations.ExcludePrefixDelete) {
                            if (name.StartsWith(excl)) {
                                shouldDelete = true;
                                break;
                            }
                        }
                    }
                    
                    if (!shouldDelete) {
                        AnsiConsole.WriteLine($"Keeping Scripts/{name}");
                        continue;
                    }
                    
                    AnsiConsole.MarkupLine($"[red]Deleting[/] Scripts/{name}");
                    
                    // make sure we don't exclude a required path
                    var dirFiles   = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                    var deletedAll = true;
                    foreach (var file in dirFiles) {
                        // var localPath = Path.Combine(file[(projectPath.Length + 1)..])
                        //     .Replace('\\', '/');
                        // if (safeFiles.Contains(localPath) || safeFiles.Any(x => localPath.StartsWith(x))) {
                        //     Console.WriteLine($" - keeping {localPath}");
                        //     deletedAll = false;
                        //     continue;
                        // }
                        
                        // Console.WriteLine($" - deleted {localPath}");
                        // File.Delete(file);
                        
                        if (safePaths.Contains(file) || safePaths.Any(x => file.StartsWith(x))) {
                            Console.WriteLine($" - keeping {file}");
                            deletedAll = false;
                            continue;
                        }
                        
                        Console.WriteLine($" - deleted {file}");
                        File.Delete(file);
                    }
                    
                    if (deletedAll) {
                        Directory.Delete(dir, true);
                    }
                }
            });
        
        AnsiConsole.MarkupLine("[green]Done[/] checking folders");
    }
    
    public static void RemoveEditorFolderFromProject(string projectPath) {
        AnsiConsole.MarkupLine($"[red]Deleting[/] editor folder from \"{projectPath}\"");
        
        var editorFolderPath = Utility.GetEditorScriptFolder(projectPath);
        if (Directory.Exists(editorFolderPath)) {
            Directory.Delete(editorFolderPath, true);
        }
        
        AnsiConsole.MarkupLine("[green]Removed[/] editor folder");
    }
    
    public static void RemoveLibraryFolders(string projectPath) {
        var libraryFolder = Path.Combine(projectPath, "Library");
        if (!Directory.Exists(libraryFolder)) return;
        
        AnsiConsole.MarkupLine($"[red]Deleting[/] Library folder from \"{projectPath}\"");
        Directory.Delete(libraryFolder, true);
    }
}

public record ExtractSettings {
    // import
    [TomlPrecedingComment(@"Options:
Level 0 = Scripts are not loaded.
Level 1 = Methods are stubbed during processing.
Level 2 = This level is the default. It has full methods for Mono games and empty methods for IL2Cpp games.
Level 3 = IL2Cpp methods are safely recovered where possible.
Level 4 = IL2Cpp methods are recovered without regard to safety. Currently the same as Level2.")]
    public required ScriptContentLevel ScriptContentLevel { get; set; }
    [TomlPrecedingComment(@"Options:
Ignore
Extract")]
    public required StreamingAssetsMode StreamingAssetsMode { get; set; }
    public required bool IgnoreStreamingAssets { get; set; }
    
    // export
    [TomlPrecedingComment(@"Options:
Yaml      = Export as a yaml asset and resS file. This is a safe option and is the backup when things go wrong.
Native    = For advanced users. This exports in a native format, usually FSB (FMOD Sound Bank). FSB files cannot be used in Unity Editor.
Default   = This is the recommended option. Audio assets are exported in the compression of the source, usually OGG.
PreferWav = Not advised if rebundling. This converts audio to the WAV format when possible.")]
    public required AudioExportFormat AudioExportFormat { get; set; }
    [TomlPrecedingComment(@"Options:
Bytes = Export as bytes.
Txt   = Export as plain text files.
Parse = Export as plain text files, but try to guess the file extension.")]
    public required TextExportMode TextExportMode { get; set; }
    [TomlPrecedingComment(@"Options:
Yaml      = Export as yaml assets which can be viewed in the editor.
            This is the only mode that ensures a precise recovery of all metadata of sprites.
            * See: https://github.com/trouger/AssetRipper/issues/2
Native    = Export in the native asset format, where all sprites data are stored in texture importer settings.
            The output from this mode was substantially changed by https://github.com/AssetRipper/AssetRipper/commit/084b3e5ea7826ac2f54ed2b11cbfbbf3692ddc9c
            Using this is inadvisable.
Texture2D = Export as a Texture2D png image.
            The output from this mode was substantially changed by https://github.com/AssetRipper/AssetRipper/commit/084b3e5ea7826ac2f54ed2b11cbfbbf3692ddc9c
            Using this is inadvisable.")]
    public required SpriteExportMode SpriteExportMode { get; set; }
    [TomlPrecedingComment(@"Options:
Dummy       = Export as dummy shaders which compile in the editor.
Yaml        = Export as yaml assets which can be viewed in the editor.
Disassembly = Export as disassembly which does not compile in the editor.
Decompile   = Export as decompiled hlsl (unstable!).")]
    public required ShaderExportMode ShaderExportMode { get; set; }
    [TomlPrecedingComment(@"Options:
AutoExperimental
AutoSafe
CSharp1
CSharp2
CSharp3
CSharp4
CSharp5
CSharp6
CSharp7
CSharp7_1
CSharp7_2
CSharp7_3
CSharp8_0
CSharp9_0
CSharp10_0
CSharp11_0
CSharp12_0
Latest")]
    public required ScriptLanguageVersion ScriptLanguageVersion { get; set; }
    [TomlPrecedingComment(@"Options:
Decompiled               = Use the ILSpy decompiler to generate CS scripts. This is reliable. However, it's also time-consuming and contains many compile errors.
Hybrid                   = Special assemblies, such as Assembly-CSharp, are decompiled to CS scripts with the ILSpy decompiler. Other assemblies are saved as DLL files.
DllExportWithRenaming    = Special assemblies, such as Assembly-CSharp, are renamed to have compatible names.
DllExportWithoutRenaming = Export assemblies in their compiled Dll form. Experimental. Might not work at all.")]
    public required ScriptExportMode ScriptExportMode { get; set; }
    [TomlPrecedingComment(@"Options:
Exr
Image
Yaml  = The internal Unity format")]
    public required LightmapTextureExportFormat LightmapTextureExportFormat { get; set; }
    [TomlPrecedingComment(@"Options:
Bmp  = Lossless. Bitmap.
Exr  = Lossless. OpenEXR.
Hdr  = Lossless. Radiance HDR.
Jpeg = Lossy.    Joint Photographic Experts Group.
Png  = Lossless. Portable Network Graphics.
Tga  = Lossless. Truevision TGA.")]
    public required ImageExportFormat ImageExportFormat { get; set; }
    
    // processing
    [TomlPrecedingComment("Only available with AssetRipper Premium.")]
    public required bool EnablePrefabOutlining { get; set; }
    [TomlPrecedingComment("Only available with AssetRipper Premium.")]
    public required bool EnableStaticMeshSeparation { get; set; }
    [TomlPrecedingComment("Only available with AssetRipper Premium.")]
    public required bool EnableAssetDeduplication { get; set; }
//     [TomlPrecedingComment(@"Options:
// GroupByAssetType  = Bundled assets are treated the same as assets from other files.
// GroupByBundleName = Bundled assets are grouped by their asset bundle name.
//                     For example: Assets/Asset_Bundles/NameOfAssetBundle/InternalPath1/.../InternalPathN/assetName.extension
// DirectExport      = Bundled assets are exported without grouping.
//                     For example: Assets/InternalPath1/.../InternalPathN/bundledAssetName.extension")]
//     public required BundledAssetsExportMode BundledAssetsExportMode { get; set; }
    
    public static ExtractSettings Default {
        get {
            var import     = new ImportSettings();
            var export    = new ExportSettings();
            var processing = new ProcessingSettings();
            
            return new ExtractSettings() {
                ScriptContentLevel          = import.ScriptContentLevel,
                StreamingAssetsMode         = import.StreamingAssetsMode,
                IgnoreStreamingAssets       = import.IgnoreStreamingAssets,
                
                AudioExportFormat           = export.AudioExportFormat,
                TextExportMode              = export.TextExportMode,
                SpriteExportMode            = export.SpriteExportMode,
                ShaderExportMode            = export.ShaderExportMode,
                ScriptLanguageVersion       = export.ScriptLanguageVersion,
                // ScriptExportMode            = export.ScriptExportMode,
                ScriptExportMode            = ScriptExportMode.Decompiled,
                LightmapTextureExportFormat = export.LightmapTextureExportFormat,
                ImageExportFormat           = export.ImageExportFormat,
                
                EnablePrefabOutlining       = processing.EnablePrefabOutlining,
                EnableStaticMeshSeparation  = processing.EnableStaticMeshSeparation,
                EnableAssetDeduplication    = processing.EnableAssetDeduplication,
                // BundledAssetsExportMode     = processing.BundledAssetsExportMode
            };
        }
    }
    
    public ExtractSettings() { }
    
    public static ExtractSettings FromConfig(LibraryConfiguration config) {
        return new ExtractSettings() {
            ScriptContentLevel          = config.ImportSettings.ScriptContentLevel,
            StreamingAssetsMode         = config.ImportSettings.StreamingAssetsMode,
            IgnoreStreamingAssets       = config.ImportSettings.IgnoreStreamingAssets,
                    
            AudioExportFormat           = config.ExportSettings.AudioExportFormat,
            TextExportMode              = config.ExportSettings.TextExportMode,
            SpriteExportMode            = config.ExportSettings.SpriteExportMode,
            ShaderExportMode            = config.ExportSettings.ShaderExportMode,
            ScriptLanguageVersion       = config.ExportSettings.ScriptLanguageVersion,
            // ScriptExportMode            = export.ScriptExportMode,
            ScriptExportMode            = ScriptExportMode.Decompiled,
            LightmapTextureExportFormat = config.ExportSettings.LightmapTextureExportFormat,
            ImageExportFormat           = config.ExportSettings.ImageExportFormat,
                    
            EnablePrefabOutlining       = config.ProcessingSettings.EnablePrefabOutlining,
            EnableStaticMeshSeparation  = config.ProcessingSettings.EnableStaticMeshSeparation,
            EnableAssetDeduplication    = config.ProcessingSettings.EnableAssetDeduplication,
            // BundledAssetsExportMode     = config.ProcessingSettings.BundledAssetsExportMode,
        };
    }
    
    public static ExtractSettings Disabled {
        get {
            return new ExtractSettings() {
                ScriptContentLevel          = default,
                StreamingAssetsMode         = default,
                IgnoreStreamingAssets       = default,
                        
                AudioExportFormat           = default,
                TextExportMode              = default,
                SpriteExportMode            = default,
                ShaderExportMode            = default,
                ScriptLanguageVersion       = default,
                ScriptExportMode            = default,
                LightmapTextureExportFormat = default,
                ImageExportFormat           = default,
                        
                EnablePrefabOutlining       = default,
                EnableStaticMeshSeparation  = default,
                EnableAssetDeduplication    = default,
                // BundledAssetsExportMode     = default,
            };
        }
    }
}
