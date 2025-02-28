using System.Reflection;
using Tomlet;
using Tomlet.Attributes;

namespace Nomnom;


public record GameSettings {
    public required PackageOverrides PackageOverrides { get; set; }
    
    public static string GetGameName(string gameName) {
        return gameName.Replace(" ", "_")
            .ToLower();
    }
    
    public static string GetSavePath(string gameName) {
        var exePath = Assembly.GetEntryAssembly()?.Location;
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        gameName = GetGameName(gameName);
            
        var outputPath = Path.GetFullPath(
            Path.Combine(
                exePath,
                "..",
                $"settings_game_{gameName}.toml"
            )
        );
        
        return outputPath;
    }
    
    public static readonly GameSettings Default = new() {
        // ExtractSettings  = null,
        PackageOverrides = new() {
            Packages = []
        }
    };
    
    public static GameSettings? Load(string gameName) {
        var path = GetSavePath(gameName);
        if (!File.Exists(path)) {
            Save(Default, gameName);
            return null;
        }
        
        // load contents
        var contents = File.ReadAllText(path);
        var settings = TomletMain.To<GameSettings>(contents);
        
        Save(settings, gameName);
        
        return settings;
    }
    
    public static void Save(GameSettings settings, string gameName) {
        var path = GetSavePath(gameName);
        
        var contents = TomletMain.TomlStringFrom(settings);
        File.WriteAllText(path, contents);
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
