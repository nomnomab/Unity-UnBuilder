namespace Nomnom;

public static class ApplyFixes {
    public static async Task FixAll(AppSettings appSettings, GameSettings gameSettings, ExtractData extractData, PackageTree packageTree, UnityPath unityPath) {
        var tempProjectPath = extractData.GetProjectPath();
        
        var defines = new List<string>();
        var files   = new List<string>();
        
        if (packageTree.Find("com.unity.inputsystem") != null) {
            defines.Add("FIX_INPUT_SYSTEM");
            
            files.Add(Utility.CopyOverScript(tempProjectPath, "FixInputSystemActions"));
        }
        
        var filePath = Utility.CopyOverScript(tempProjectPath, "InitProject", x => {
            // defines
            x = x.Replace("#_DEFINES_", string.Join('\n', defines.Select(y => $"#define {y}")));
            return x;
        });
        files.Add(filePath);
        
        await UnityCLI.OpenProject("Finishing some final tasks", unityPath, false, extractData.GetProjectPath(),
            "-executeMethod Nomnom.InitProject.OnLoad",
            "-exit"
        );
        
        await Task.Delay(500);
        
        foreach (var file in files) {
            File.Delete(file);
        }
    }
}
