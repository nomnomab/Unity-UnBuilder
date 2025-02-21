using System.Reflection;
using System.Text.Json;

namespace Nomnom;

public record Settings {
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
    
    private static readonly Settings Default = new() {
        UnityInstallsFolder = "",
    };
    
    public static Settings? Load() {
        var path = SavePath;
        if (!File.Exists(path)) {
            var tmp = JsonSerializer.Serialize(Default);
            File.WriteAllText(path, tmp);
        }
        
        var contents = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<Settings>(contents);
        return settings;
    }
    
    public static void Save(Settings settings) {
        var path     = SavePath;
        var contents = JsonSerializer.Serialize(settings);
        File.WriteAllText(path, contents);
    }
}
