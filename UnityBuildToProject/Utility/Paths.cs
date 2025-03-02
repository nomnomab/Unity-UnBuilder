using System.Reflection;

namespace Nomnom;

public static class Paths {
    public static string ExeFolder {
        get {
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException(exePath);
            }
            
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    exePath,
                    ".."
                )
            );
            
            return outputPath;
        }
    }
    
    public static string ResourcesFolder {
        get {
            var path = Path.Combine(ExeFolder, "Resources");
            EnsureDirectory(path);
            return path;
        }
    }
    
    public static string LogsFolder {
        get {
            var path = Path.Combine(ExeFolder, "logs");
            EnsureDirectory(path);
            return path;
        }
    }
    
    public static string LibFolder {
        get {
            var path = Path.Combine(ExeFolder, "lib");
            EnsureDirectory(path);
            return path;
        }
    }
    
    private static void EnsureDirectory(string path) {
        Directory.CreateDirectory(path);
    }
}
