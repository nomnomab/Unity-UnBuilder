using Tomlet.Attributes;

namespace Nomnom;


public record GameSettings {
    public required PackageOverrides PackageOverrides { get; set; }
    public required FileOverrides FileOverrides { get; set; }
    
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
        // ExtractSettings  = null,
        PackageOverrides = new() {
            Packages = []
        },
        FileOverrides = new() {
            ProjectPaths = [],
        }
    };
    
    public static GameSettings? Load(string gameName) {
        var savePath = GetSavePath(gameName);
        var settings = Settings.Load(savePath, Default, null);
        
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
}

public record PackageOverride(string Id, string? Version);

public record FileOverrides {
    [TomlPrecedingComment(@"Files and folders that will make sure to be included in the final project.

You really only need to do this for things like custom scripts inside of an internal package namespace folder.

Each entry is in the format of:
{ Path = ""Asset/FileOrFolder"" },")]
    public required FileOverride[] ProjectPaths = [];
}

public record FileOverride(string Path);
