namespace Nomnom;

public static class FixInputSystem {
    /// <summary>
    /// Converts the ripped actions into the actual working meta file and
    /// the action JSON it expects.
    /// </summary>
    public static async Task FixActionsAssets(ExtractData extractData, UnityPath unityPath) {
        var projectPath = extractData.GetProjectPath();
        var file        = Utility.CopyOverScript(projectPath, "FixInputSystemActions");
            
        await UnityCLI.OpenProject("Fixing the Input System", unityPath, false, extractData.GetProjectPath(),
            "-executeMethod Nomnom.FixInputSystemActions.Fix",
            "-quit"
        );
        
        File.Delete(file);
    }
}
