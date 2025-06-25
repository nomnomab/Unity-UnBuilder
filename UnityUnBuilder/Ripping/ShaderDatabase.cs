namespace Nomnom;

/// <summary>
/// Stores all of the needed shader paths with their names.
/// </summary>
public record ShaderDatabase {
    public required Dictionary<UnityGuid, ShaderFile> Shaders { get; set; }
    public required Dictionary<string, UnityGuid> FilePathToGuid { get; set; }
    public required Dictionary<string, UnityGuid> NameToGuid { get; set; }
    
    public static ShaderDatabase Parse(string folderPath, GuidDatabase guidDatabase) {
        var db = new ShaderDatabase() {
            Shaders        = [],
            FilePathToGuid = [],
            NameToGuid     = []
        };
        
        var files = GetFiles(folderPath);
        foreach (var file in files) {
            if (!guidDatabase.FilePathToGuid.TryGetValue(file, out var guid)) {
                continue;
            }
            
            var shaderFile = UnityAssetTypes.ParseShaderFile(file);
            if (shaderFile == null) continue;
            
            db.Shaders.Add(guid, shaderFile);
            db.FilePathToGuid.Add(file, guid);
            db.NameToGuid.Add(shaderFile.Name, guid);
        }
        
        return db;
    }
    
    private static IEnumerable<string> GetFiles(string folderPath) {
        return Directory.EnumerateFiles(folderPath, "*.shader", SearchOption.AllDirectories);
    }
    
    public static void FindFinalShaderFiles(ShaderDatabase[] shaderDatabases) {
        
    }
    
    public static void FindGuids(ShaderDatabase shaderDatabase, GuidDatabase guidDatabase) {
        foreach (var (guid, shaderFile) in shaderDatabase.Shaders) {
            
        }
    }
}
