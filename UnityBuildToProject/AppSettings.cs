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
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException(exePath);
            }
            
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    exePath,
                    "..",
                    "settings.toml"
                )
            );
            
            return outputPath;
        }
    }
    
    public static readonly AppSettings Default = new() {
        UnityHubFolder      = new CrossPlatformString(
            windows: "C:/Program Files/Unity Hub", 
            unix   : "~/Applications/Unity\\ Hub.AppImage", 
            macOs  : "/Applications/Unity\\ Hub.app/Contents/MacOS/Unity\\ Hub"
        ).GetValue(),
        
        UnityInstallsFolder = new CrossPlatformString(
            windows: "C:/Program Files/Unity/Hub/Editor", 
            unix   : "",
            macOs  : ""
        ).GetValue(),
        
        ExtractSettings     = ExtractSettings.Default,
    };
    
    public static AppSettings? Load() {
        var path = SavePath;
        if (!File.Exists(path)) {
            Save(Default);
            return null;
        }
        
        // load contents
        var contents = File.ReadAllText(path);
        var settings = TomletMain.To<AppSettings>(contents);
        
        // verify values are assigned
        if (!Directory.Exists(settings.UnityInstallsFolder)) {
            throw new InvalidAppSettingsFieldException(nameof(UnityInstallsFolder));
        }
        
        Save(settings);
        
        return settings;
    }
    
    public static void Save(AppSettings settings) {
        var path   = SavePath;
        
        var contents = TomletMain.TomlStringFrom(settings);
        File.WriteAllText(path, contents);
    }
    
    public static void Validate(AppSettings settings) {
        if (string.IsNullOrEmpty(settings.UnityHubFolder)) {
            throw new Exception("UnityHubFolder was not assigned in the settings.toml");
        }
        
        if (string.IsNullOrEmpty(settings.UnityInstallsFolder)) {
            throw new Exception("UnityInstallsFolder was not assigned in the settings.toml");
        }
    }
}

sealed class InvalidAppSettingsFieldException(string fieldName)
    : Exception($"Assign the \"{fieldName}\" value in the settings.json!") { }
