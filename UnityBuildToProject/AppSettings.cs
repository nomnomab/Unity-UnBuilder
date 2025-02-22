using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Nomnom;

public record AppSettings {
    /// <summary>
    /// Where the Unity Hub is located.
    /// <br/><br/>
    /// On Windows this is typically: "C:\Program Files\Unity Hub"
    /// </summary>
    public required string UnityHubFolder { get; set; }
    
    /// <summary>
    /// The folder that contains all of the unity installations.
    /// <br/><br/>
    /// On Windows this is typically: "C:\Program Files\Unity\Hub\Editor".
    /// </summary>
    public required string UnityInstallsFolder { get; set; }
    
    private static string SavePath {
        get {
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException(exePath);
            }
            
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    exePath,
                    "..",
                    "settings.json"
                )
            );
            
            return outputPath;
        }
    }
    
    private static readonly AppSettings Default = new() {
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
    };
    
    public static AppSettings? Load() {
        var path = SavePath;
        if (!File.Exists(path)) {
            Save(Default);
        }
        
        // load contents
        var contents = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(contents)!;
        
        // verify values are assigned
        if (!Directory.Exists(settings.UnityInstallsFolder)) {
            throw new InvalidAppSettingsFieldException(nameof(UnityInstallsFolder));
        }
        
        return settings;
    }
    
    public static void Save(AppSettings settings) {
        var path   = SavePath;
        var config = new JsonSerializerOptions(JsonSerializerDefaults.General) {
            WriteIndented = true
        };

        var contents = JsonSerializer.Serialize(settings, config);
        File.WriteAllText(path, contents);
    }
}

sealed class InvalidAppSettingsFieldException(string fieldName)
    : Exception($"Assign the \"{fieldName}\" value in the settings.json!") { }
