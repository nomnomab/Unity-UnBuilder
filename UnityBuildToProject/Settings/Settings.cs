using Tomlet;

namespace Nomnom;

public static class Settings {
    public static string FolderPath {
        get {
            var path = Path.Combine(Paths.ExeFolder, "settings");
            Directory.CreateDirectory(path);
            return path;
        }
    }
    
    public static T? Load<T>(string savePath, T defaultValue, Action<T>? verify = null) {
        if (!File.Exists(savePath)) {
            Save(savePath, defaultValue);
            return default;
        }
        
        // load contents
        var contents = File.ReadAllText(savePath);
        var settings = TomletMain.To<T>(contents);
        
        if (verify != null) {
            verify(settings);
        }
        
        Save(savePath, settings);
        return settings;
    }
    
    public static void Save<T>(string savePath, T settings) {
        var saveFolder = Path.GetDirectoryName(savePath);
        Directory.CreateDirectory(saveFolder!);
        
        var contents = TomletMain.TomlStringFrom(settings);
        File.WriteAllText(savePath, contents);
    }
}
