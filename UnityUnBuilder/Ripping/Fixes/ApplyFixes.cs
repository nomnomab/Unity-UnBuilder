namespace Nomnom;

public static class ApplyFixes {
    public static async Task FixBeforeGuids(ToolSettings settings) {
        await FixTextMeshPro.ImportTextMeshProEssentials(settings);
        await FixFiles.CopyOverCustomFiles(settings);
        await FixFiles.ImportCustomUnityPackages(settings);
    }

    public static void FixBeforeRecompile(ToolSettings settings, PackageTree? packageTree) {
        if (packageTree != null) {
            if (packageTree.Find("com.unity.netcode.gameobjects") != null) {
                FixUnityNGO.RevertGeneratedCode(settings);
            }
            
            FixFiles.FixAmbiguousUsages(settings);
            FixFiles.FixCheckedGetHashCodes(settings);
            FixFiles.RemovePrivateDetails(settings);
            FixFiles.ReplaceFileContents(settings);
        }
        
        // todo: extract this game specific
        FixTextures.FixFormat(settings.ExtractData.GetProjectPath(), null, x => {
            var name = Path.GetFileNameWithoutExtension(x);
            if (name.StartsWith("LDR_RGB")) {
                return AssetRipper.SourceGenerated.Enums.TextureFormat.RGB24;
            }
            
            return null;
        });
        
        FixFiles.FixAssetNames(settings);
        FixFiles.CleanupDeadMetaFiles(settings);
    }
    
    public static async Task FixAfterRecompile(ToolSettings settings, GuidDatabase guidDatabase, PackageTree? packageTree) {
        var extractData = settings.ExtractData;
        var unityPath   = settings.GetUnityPath();
        
        if (packageTree != null) {
            if (packageTree.Find("com.unity.inputsystem") != null) {
                await FixInputSystem.FixActionsAssets(extractData, unityPath);
            }
            
            if (packageTree.Find("com.unity.addressables") != null) {
                // await FixAddressables.InstallAddressables(settings, guidDatabase);
            }
        }
        
        FixFiles.ParseTextFiles(extractData);
        FixFiles.FixShaders(extractData);
        FixFiles.FixDuplicateAssets(settings);
        
        await Task.Delay(500);
    }
}
