using Spectre.Console;

namespace Nomnom;

public static class GuidMapping {
    public static async Task<GuidDatabase> ExtractGuids(string projectPath) {
        Console.WriteLine("Extracting guids from project");
        var db = GuidDatabase.Parse(projectPath);
        Console.WriteLine("Extracted guids from project.");
        await Task.Delay(1000);
        
        if (db == null) {
            throw new Exception("Failed to parse guid database");
        }
        
        return db;
    }
    
    public static string? FindDll(string projectPath, string dllName) {
        var scriptsPath = Path.Combine(projectPath, "Assets", "Scripts");
        
        var dllNameNoExtension = dllName.EndsWith(".dll") ? Path.GetFileNameWithoutExtension(dllName) : dllName;
        var dllPath            = Path.Combine(scriptsPath, dllNameNoExtension);
        if (!Directory.Exists(dllPath)) {
            return null;
        }
        
        return dllPath;
    }
}
