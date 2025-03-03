namespace Nomnom;

public static class ApplyFixes {
    public static async Task FixAll(AppSettings appSettings, GameSettings gameSettings, ExtractData extractData, PackageTree packageTree, UnityPath unityPath) {
        if (packageTree.Find("com.unity.inputsystem") != null) {
            await FixInputSystemActions(extractData, unityPath);
        }
        
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
}
