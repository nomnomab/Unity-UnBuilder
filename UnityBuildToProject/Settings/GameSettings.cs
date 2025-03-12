using Tomlet.Attributes;

namespace Nomnom;

public record GameSettings {
    public required GeneralSettings General { get; set; }
    public required PackageOverrides PackageOverrides { get; set; }
    public required FileOverrides FileOverrides { get; set; }
    public required FileCopying FileCopying { get; set; }
    
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
        PackageOverrides = new() {
            Packages            = [],
            ImportUnityPackages = [],
        },
        FileOverrides = new() {
            ProjectPaths = [],
            Exclusions   = [],
        },
        FileCopying = new() {
            FilePaths    = [],
        }
    };
    
    public static GameSettings? Load(string gameName) {
        var savePath = GetSavePath(gameName);
        var settings = Settings.Load(savePath, Default, x => {
            var defaultValue = Default;
            x.General          ??= defaultValue.General;
            x.PackageOverrides ??= defaultValue.PackageOverrides;
            x.FileOverrides    ??= defaultValue.FileOverrides;
            x.FileCopying      ??= defaultValue.FileCopying;
        });
        
        return settings;
    }
    
    public static void Save(AppSettings settings, string gameName) {
        var savePath = GetSavePath(gameName);
        Settings.Save(savePath, settings);
    }
}

public record PackageOverrides {
    [TomlPrecedingComment(@"Add new packages, or override a package version here.

Each entry is in the format of:
{ Id = ""com.package.name"", Version = ""1.0.0"", },

A version of ""no"" will exclude the package if it is included.")]
    public required PackageOverride[] Packages = [];
    
    [TomlPrecedingComment(@"Import .unitypackage files from the project path.

Each entry is in the format of:
{ Path = ""Assets/File.unitypackage"", },")]
    public required ImportUnityPackage[] ImportUnityPackages = [];
}

public record PackageOverride(string Id, string? Version);

public record ImportUnityPackage(string Path);

public record FileOverrides {
    [TomlPrecedingComment(@"Files and folders that will make sure to be included in the final project.

You really only need to do this for things like custom scripts inside of an internal package namespace folder.

Each entry is in the format of:
{ Path = ""Assets/FileOrFolder"" },")]
    public FileOverride[]? ProjectPaths = [];
    
    [TomlPrecedingComment(@"Files and folders that will NOT be included in the final project.

Each entry is in the format of:
{ Path = ""Assets/FileOrFolder"" },

Or in the format of this to exclude files with a prefix:
{ Path = ""Assets/File.*"" },")]
    public FileOverride[]? Exclusions = [];
}

public record FileOverride(string Path);

public record FileCopying {
    [TomlPrecedingComment(@"Files that will be copied to the project.

Tags can be used to indicate locations:
> $DATA$    : Game/Game_Data/
> $MANAGED$ : Game/Game_Data/Managed
> $PLUGINS$ : Game/Game_Data/Plugins

Each entry is in the format of:
{ PathFrom = ""Path/To/File"", PathTo = ""Assets/File"" },")]
    public required FileCopy[] FilePaths = [];
}

public record FileCopy(string PathFrom, string PathTo);

public record GeneralSettings {
    [TomlPrecedingComment(@"The path to the scene file to open on completion.")]
    public required string? OpenScenePath { get; set; }
}
