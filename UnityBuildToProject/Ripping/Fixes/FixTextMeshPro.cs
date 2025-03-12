namespace Nomnom;

public static class FixTextMeshPro {
    /// <summary>
    /// Imports the TextMeshPro essentials package.
    /// </summary>
    public static async Task ImportTextMeshProEssentials(ToolSettings settings) {
        if (settings.GameData.ProjectVersion.LessThan(2018)) return;
        
        var projectPath  = settings.ExtractData.GetProjectPath();
        var packagesPath = Path.Combine(projectPath, "Library", "PackageCache");
        var tmpPaths     = Directory.GetDirectories(packagesPath, "com.unity.textmeshpro@*", SearchOption.TopDirectoryOnly);
        var tmpPath      = tmpPaths.FirstOrDefault();
        
        if (tmpPath == null) {
            throw new FileNotFoundException("No com.unity.textmeshpro folder found");
        }
        
        var packagePath = Path.GetFullPath(
            Path.Combine(tmpPath, "Package Resources", "TMP Essential Resources.unitypackage")
        );
        
        await FixFiles.ImportUnityPackage(settings, packagePath);
    }
}
