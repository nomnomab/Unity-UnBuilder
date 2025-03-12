using System.Text.Json;
using Spectre.Console;

namespace Nomnom;

public static class GuidMapping {
    public static async Task<GuidDatabase> ExtractGuids(ToolSettings settings, string projectPath) {
        Console.WriteLine("Extracting guids from project");
        var db = GuidDatabase.Parse(projectPath);
        Console.WriteLine("Extracted guids from project.");
        await Task.Delay(1000);
        
        if (db == null) {
            throw new Exception("Failed to parse guid database");
        }
        
        return db;
    }
    
    public static async Task ExtractDllGuids(ToolSettings settings, GuidDatabase db, RoslynDatabase typeDb, string projectPath) {
        var unityPath       = settings.GetUnityPath();
        Utility.CopyOverScript(projectPath, "ScrubDllGuids");
        
        await UnityCLI.OpenProjectHidden("Opening project to grab dll guids", unityPath, true, projectPath,
            "-executeMethod Nomnom.ScrubDllGuids.OnLoad"
        );
        
        // now parse the file the extractor created
        var filePath = Path.Combine(projectPath, "..", "dlls_output.json");
        if (!File.Exists(filePath)) {
            if (settings.ProgramArgs.SkipPackageFetching) {
                AnsiConsole.MarkupLine("[red]Error[/]: No dlls_output.json found?");
            }
            throw new FileNotFoundException(filePath);
        }
        
        // get the package list
        var dllsOutputJson = File.ReadAllText(filePath);
        var dllsOutput     = JsonSerializer.Deserialize<ScrubbedDlls>(dllsOutputJson);
        if (dllsOutput == null) {
            throw new FileNotFoundException(filePath);
        }
        
        foreach (var value in dllsOutput.Dlls) {
            Console.WriteLine($"[from dll] {value.FullName}:{value.Guid}:{value.FileID}:{value.FileType}:{value.Path}");
            
            if (value.Path != null) {
                var dllRef = new UnityDllReference {
                    TypeName = value.FullName,
                    FilePath = value.Path,
                    Ref      = new UnityAssetReference {
                        FileId = new UnityFileId(value.FileID),
                        Guid   = new UnityGuid(value.Guid),
                        Type   = new UnityFileType(long.Parse(value.FileType))
                    }
                };
                
                if (db.DllReferences.Add(dllRef)) {
                    Console.WriteLine($" - registered to {value.Path}");
                }
            }
        }
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
    
    [Serializable]
    class ScrubbedDlls {
        public List<ScrubbedDll> Dlls { get; set; } = [];
    }
    
    [Serializable]
    class ScrubbedDll {
        public string FullName { get; set; }
        public string Path { get; set; }
        public string Guid { get; set; }
        public string FileID { get; set; }
        public string FileType { get; set; }
    }
}
