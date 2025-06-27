using System.Reflection;
using Tomlet;
using Tomlet.Attributes;

namespace Nomnom;

public record AppSettings {
    /// <summary>
    /// Where the Unity Hub is located.
    /// <br/><br/>
    /// On Windows this is typically: "C:/Program Files/Unity Hub"
    /// </summary>
    [TomlPrecedingComment(@"Where the Unity Hub is located.

On Windows this is typically: ""C:/Program Files/Unity Hub"".")]
    public string? UnityHubFolder { get; set; }
    
    /// <summary>
    /// The folder that contains all of the unity installations.
    /// <br/><br/>
    /// On Windows this is typically: "C:/Program Files/Unity/Hub/Editor".
    /// </summary>
    [TomlPrecedingComment(@"The folder that contains all of the unity installations.

On Windows this is typically: ""C:/Program Files/Unity/Hub/Editor"".")]
    public string? UnityInstallsFolder { get; set; }
    
    public ExtractSettings? ExtractSettings { get; set; }
    
    public static string SavePath {
        get {
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    Paths.SettingsFolder,
                    "settings.toml"
                )
            );
            
            return outputPath;
        }
    }
    
    public static readonly AppSettings Default = new() {
        UnityHubFolder      = new CrossPlatformString(
            Windows: "C:/Program Files/Unity Hub", 
            Unix   : "~/Applications/Unity\\ Hub.AppImage", 
            MacOs  : "/Applications/Unity\\ Hub.app/Contents/MacOS/Unity\\ Hub"
        ).GetValue(),
        
        UnityInstallsFolder = new CrossPlatformString(
            Windows: "C:/Program Files/Unity/Hub/Editor", 
            Unix   : "",
            MacOs  : ""
        ).GetValue(),
        
        ExtractSettings     = ExtractSettings.Default,
    };
    
    public static AppSettings? Load() {
        var settings = Settings.Load(SavePath, Default, Validate);
        return settings;
    }
    
    public static void Save(AppSettings settings) {
        Settings.Save(SavePath, settings);
    }
    
    public static void Validate(AppSettings settings) {
        if (string.IsNullOrEmpty(settings.UnityHubFolder)) {
            throw new Exception("UnityHubFolder was not assigned in the settings.toml");
        }
        
        if (string.IsNullOrEmpty(settings.UnityInstallsFolder)) {
            throw new Exception("UnityInstallsFolder was not assigned in the settings.toml");
        }
        
        if (!Directory.Exists(settings.UnityInstallsFolder)) {
            throw new InvalidAppSettingsFieldException(nameof(UnityInstallsFolder));
        }
    }
}

sealed class InvalidAppSettingsFieldException(string fieldName)
    : Exception($"Assign the \"{fieldName}\" value in the settings.json!") { }
