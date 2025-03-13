using Tomlet.Attributes;

namespace Nomnom;

public record GameSettings {
    public required GeneralSettings General { get; set; }
    public required Packages Packages { get; set; }
    public required Files Files { get; set; }
    
    public static string GetGameName(string gameName) {
        return gameName.Replace(" ", "_")
            .ToLower();
    }
    
    public static string GetSaveFolder(string gameName) {
        gameName = GetGameName(gameName);
            
        var outputPath = Path.GetFullPath(
            Path.Combine(
                Settings.FolderPath,
                gameName
            )
        );
        
        return outputPath;
    }
    
    public static string GetSavePath(string gameName) {
        gameName = GetGameName(gameName);
            
        var outputPath = Path.GetFullPath(
            Path.Combine(
                GetSaveFolder(gameName),
                "settings.toml"
            )
        );
        
        return outputPath;
    }
    
    public static readonly GameSettings Default = new() {
        General = new() {
            OpenScenePath = null,
        },
        Packages = new() {
            Overrides           = [],
            ImportUnityPackages = [],
        },
        Files = new() {
            CopyFilePaths       = [],
            PathExclusions  = [],
            IncludePaths    = [],
            ReplaceContents = [],
        }
    };
    
    public static GameSettings? Load(string gameName) {
        var savePath = GetSavePath(gameName);
        var settings = Settings.Load(savePath, Default, x => {
            var defaultValue = Default;
            x.General          ??= defaultValue.General;
            x.Packages ??= defaultValue.Packages;
            x.Files            ??= defaultValue.Files;
        });
        
        return settings;
    }
    
    public static void Save(AppSettings settings, string gameName) {
        var savePath = GetSavePath(gameName);
        Settings.Save(savePath, settings);
    }
}

public record GeneralSettings {
    [TomlPrecedingComment(@"The path to the scene file to open on completion.")]
    public required string? OpenScenePath { get; set; }
}

public record Packages {
    [TomlPrecedingComment(@"Add new packages, or override a package version here.

Each entry is in the format of:
{ Id = ""com.package.name"", Version = ""1.0.0"", },

A version of ""no"" will exclude the package if it is included.")]
    public required PackageOverride[] Overrides = [];
    
    [TomlPrecedingComment(@"Import .unitypackage files from the project path.

Each entry is in the format of:
{ Path = ""Assets/File.unitypackage"", },")]
    public required ImportUnityPackage[] ImportUnityPackages = [];
}

public record PackageOverride(string Id, string? Version);

public record ImportUnityPackage(string Path);

public record Files {
    [TomlPrecedingComment(@"Files and folders that will make sure to be included in the final project.

You really only need to do this for things like custom scripts inside of an internal package namespace folder.

Each entry is in the format of:
{ Path = ""Assets/FileOrFolder"" },")]
    public FileOverride[]? IncludePaths = [];
    
    [TomlPrecedingComment(@"Files and folders that will NOT be included in the final project.

Each entry is in the format of:
{ Path = ""Assets/FileOrFolder"" },

Or in the format of this to exclude files with a prefix:
{ Path = ""Assets/File.*"" },")]
    public FileOverride[]? PathExclusions = [];
    
    [TomlPrecedingComment(@"Files that will be copied to the project.

Tags can be used to indicate locations:
> $DATA$    : Game/Game_Data/
> $MANAGED$ : Game/Game_Data/Managed
> $PLUGINS$ : Game/Game_Data/Plugins

Each entry is in the format of:
{ PathFrom = ""Path/To/File"", PathTo = ""Assets/File"" },")]
    public required FileCopy[] CopyFilePaths = [];
    
    [TomlPrecedingComment(@"Files that require some part of it to be replaced with something else.

The `Find` argument can also be a regex expression.

Each entry is in the format of:
{ Path = ""Assets/File"", Find = ""Foo"", Replacement = ""Bar"" }")]
    public required FileContentReplace[] ReplaceContents = [];
}

public record FileOverride(string Path);
public record FileCopy(string PathFrom, string PathTo);
public record FileContentReplace(string Path, string Find, string Replacement);
