using System.Diagnostics;
using System.Reflection;
using Spectre.Console;

namespace Nomnom;

public record GameSettings {
    public required GeneralSettings General { get; set; }
    public required Packages Packages { get; set; }
    public required Files Files { get; set; }
    
    public static string GetGameName(string gameName) {
        return gameName.Replace(" ", "_")
            .ToLower();
    }
    
    public static string GetSaveFolder(string root, string gameName) {
        gameName = GetGameName(gameName);
            
        var outputPath = Path.GetFullPath(
            Path.Combine(
                root,
                gameName
            )
        );
        
        return outputPath;
    }
    
    public static string GetSavePath(string root, string gameName) {
        gameName = GetGameName(gameName);
            
        var outputPath = Path.GetFullPath(
            GetSaveFolder(root, gameName)
        );
        
        return outputPath;
    }
    
    public static readonly GameSettings Default = new() {
        General = new() {
            OpenScenePath = null,
        },
        Packages = new() {
            // PackageList         = [],
            Overrides           = [],
            ImportUnityPackages = [],
        },
        Files = new() {
            CopyPaths                          = [],
            PathExclusions                     = [],
            IncludePaths                       = [],
            ReplaceContents                    = [],
            ReplaceAmbiguousUsages             = [],
            ScriptFolderNameExcludeFromGuids   = [],
            ScriptFolderPrefixExcludeFromGuids = [],
        }
    };
    
    public static GameSettings? Load(string settingsProjectFolder, string root, string gameName) {
        var savePath = GetSavePath(root, gameName);
        
        if (!DotNetProject.Exists(savePath)) {
            DotNetProject.New(settingsProjectFolder, root, savePath);
            
            var panel = new Panel(@$"It looks like this is your [underline]first time[/] trying to decompile ""{gameName}""!
A dotnet project was created at '{savePath}', go ahead and modify it before running the tool again.

{GameSettings.GetSavePath(root, gameName)}");
            AnsiConsole.Write(panel);
            
            Environment.Exit(0);
            return null;
        }
        
        // build and load project
        var dllPath  = DotNetProject.Build(savePath);
        var assembly = Assembly.LoadFile(dllPath);
        
        // find provider
        var providerTypeName = $"{Path.GetFileNameWithoutExtension(savePath)}.GameSettingsProvider";
        var provider         = assembly.GetType(providerTypeName);
        if (provider == null) {
            throw new Exception($"{providerTypeName} not found in your user game project!");
        }
        
        var getGameSettingsMethod = provider.GetMethod("GetGameSettings", BindingFlags.Static | BindingFlags.Public);
        if (getGameSettingsMethod == null) {
            throw new Exception($"GetGameSettings() not found in {providerTypeName}!");
        }
        
        var gameSettings = getGameSettingsMethod.Invoke(null, null);
        
        return gameSettings as GameSettings;
    }
}

public record GeneralSettings {
    /// <summary>
    /// The path to the scene file to open on completion.
    /// </summary>
    public required string? OpenScenePath { get; set; }
}

public record Packages {
    /// <summary>
    /// An entirely custom package list.
    /// If this is assigned, it will override the entire package list and skip package detection.
    /// </summary>
    // public required List<Package> PackageList { get; set; } = [];
    
    /// <summary>
    /// Add new packages, or override a package version here.
    /// </summary>
    public required List<PackageOverride> Overrides { get; set; } = [];
    
    /// <summary>
    /// Import .unitypackage files from the project path.
    /// </summary>
    public required List<ImportUnityPackage> ImportUnityPackages { get; set; } = [];
}

public record Package(string id, string version);
public record PackageOverride(string Id, string? Version, bool Exclude);
public record ImportUnityPackage(string Path);

public record Files {
    /// <summary>
    /// Files and folders that will make sure to be included in the final project.
    /// You really only need to do this for things like custom scripts inside of an internal package namespace folder.
    /// </summary>
    public List<string> IncludePaths { get; set; } = [];
    
    /// <summary>
    /// Files and folders that will NOT be included in the final project.
    /// </summary>
    public List<string> PathExclusions { get; set; } = [];
    
    /// <summary>
    /// Prefixes that will exclude certain folders in Assets/Scripts from guid mapping.
    /// </summary>
    public List<string> ScriptFolderPrefixExcludeFromGuids { get; set; } = [];
    
    /// <summary>
    /// Folder names that will be excluded in Assets/Scripts from and won't be handled in guid mapping.
    /// </summary>
    public List<string> ScriptFolderNameExcludeFromGuids { get; set; } = [];
    
    /// <summary>
    /// Files and folders that will be copied to the project.<br/>
    /// Tags can be used to indicate locations:<br/>
    /// > $DATA$    : Game/Game_Data/<br/>
    /// > $MANAGED$ : Game/Game_Data/Managed<br/>
    /// > $PLUGINS$ : Game/Game_Data/Plugins<br/>
    /// </summary>
    public required List<FileCopy> CopyPaths { get; set; } = [];
    
    /// <summary>
    /// Files that require some part of it to be replaced with something else.
    /// The `Find` argument can also be a regex expression.
    /// </summary>
    public required List<FileContentReplace> ReplaceContents { get; set; } = [];
    
    /// <summary>
    /// Used to resolve ambiguous type usages between two namespaces.
    /// </summary>
    public required List<FileAmbiguousUsage> ReplaceAmbiguousUsages { get; set; } = [];
}

public record FileCopy(string PathFrom, string PathTo);
public record FileContentReplace(string Path, string Find, string Replacement);
public record FileAmbiguousUsage(string[] Usings, string Resolution);
