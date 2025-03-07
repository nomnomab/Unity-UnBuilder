namespace Nomnom;

public static class ApplyFixes {
    public static async Task FixBeforeGuids(ToolSettings settings) {
        await FixTextMeshPro.ImportTextMeshProEssentials(settings);
        await FixFiles.CopyOverCustomFiles(settings);
        // FixFiles.FixMissingGuids(gameSettings, extractData);
    }

    public static void FixBeforeRecompile(ToolSettings settings) {
        
    }
    
    public static async Task FixAfterRecompile(ToolSettings settings, PackageTree? packageTree) {
        var extractData = settings.ExtractData;
        var unityPath   = settings.GetUnityPath();
        
        if (packageTree != null) {
            if (packageTree.Find("com.unity.inputsystem") != null) {
                await FixInputSystem.FixActionsAssets(extractData, unityPath);
            }
        }
        
        FixFiles.ParseTextFiles(extractData);
        FixFiles.FixShaders(extractData);
        FixFiles.FixDuplicateAssets(settings);
        
        await Task.Delay(500);
    }
}
