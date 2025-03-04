namespace Nomnom;

public static class ApplyFixes {
    public static async Task FixBeforeGuids(AppSettings appSettings, GameSettings gameSettings, ExtractData extractData, PackageTree packageTree, UnityPath unityPath) {
        await FixTextMeshPro(extractData, unityPath);
    }
    
    public static async Task FixAll(AppSettings appSettings, GameSettings gameSettings, ExtractData extractData, PackageTree packageTree, UnityPath unityPath) {
        if (packageTree.Find("com.unity.inputsystem") != null) {
            await FixInputSystemActions(extractData, unityPath);
        }
        
        ParseTextFiles(extractData);
        
        await Task.Delay(500);
    }
    
    public static async Task FixTextMeshPro(ExtractData extractData, UnityPath unityPath) {
        var projectPath = extractData.GetProjectPath();
        var packagesPath = Path.Combine(projectPath, "Library", "PackageCache");
        var tmpPaths     = Directory.GetDirectories(packagesPath, "com.unity.textmeshpro@*", SearchOption.TopDirectoryOnly);
        var tmpPath      = tmpPaths.FirstOrDefault();
        
        if (tmpPath == null) {
            throw new FileNotFoundException("No com.unity.textmeshpro folder found");
        }
        
        var packagePath = Path.GetFullPath(
            Path.Combine(tmpPath, "Package Resources", "TMP Essential Resources.unitypackage")
        );
        
        if (!File.Exists(packagePath)) {
            throw new FileNotFoundException(packagePath);
        }
        
        await UnityCLI.OpenProjectHidden("Fixing TextMeshPro", unityPath, true, extractData.GetProjectPath(),
            $"-importPackage \"{packagePath}\""
        );
    }
    
    public static async Task FixInputSystemActions(ExtractData extractData, UnityPath unityPath) {
        var projectPath = extractData.GetProjectPath();
        var file        = Utility.CopyOverScript(projectPath, "FixInputSystemActions");
            
        await UnityCLI.OpenProject("Fixing the Input System", unityPath, false, extractData.GetProjectPath(),
            "-executeMethod Nomnom.FixInputSystemActions.Fix",
            "-quit"
        );
        
        File.Delete(file);
    }
    
    public static void ParseTextFiles(ExtractData extractData) {
        var projectPath = extractData.GetProjectPath();
        var assetsPath  = Path.Combine(projectPath, "Assets");
        var txtFiles    = Directory.GetFiles(assetsPath, "*.txt", SearchOption.AllDirectories);
        
        foreach (var file in txtFiles) {
            Console.WriteLine($"Decoding {Utility.ClampPathFolders(file, 6)}");

            var lines = File.ReadAllLines(file);
            if (lines.Length == 0) continue;

            // todo: make this better
            // check for a .txt -> .csv
            var line = lines[0];
            if (!line.Contains(',')) continue;
            
            var csvPath = Path.ChangeExtension(file, ".csv");
            File.Move(file, csvPath, true);

            var metaPath = file + ".meta";
            if (File.Exists(metaPath)) {
                var metaCsvPath = csvPath + ".meta";
                File.Move(metaPath, metaCsvPath, true);
            }
        }
    }
}
