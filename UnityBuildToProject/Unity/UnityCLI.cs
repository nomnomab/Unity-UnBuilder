using System.Diagnostics;

namespace Nomnom;

public static class UnityCLI {
    public static void OpenProject(UnityPath unityPath, string projectPath) {
        if (!Directory.Exists(projectPath)) {
            throw new DirectoryNotFoundException(projectPath);
        }
        
        var exePath = unityPath.GetExePath();
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        // start the unity process and pass in the project to
        // open in the editor.
        var process = new Process() {
            StartInfo = new(exePath) {
                Arguments = $"-projectPath \"{projectPath}\""
            },
        };
        
        if (!process.Start()) {
            throw new Exception("Failed to start process");
        }
    }
}
