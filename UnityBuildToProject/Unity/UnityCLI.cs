using System.Diagnostics;
using Spectre.Console;

namespace Nomnom;

// https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
public static class UnityCLI {
    public static async Task CreateProject(UnityPath unityPath, string projectPath) {
        await CreateProjectWithArgs(unityPath, projectPath, []);
    }
    
    public static async Task CreateProjectWithArgs(UnityPath unityPath, string projectPath, params string[] args) {
        if (!Directory.Exists(projectPath)) {
            throw new DirectoryNotFoundException(projectPath);
        }
        
        var exePath = unityPath.GetExePath();
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        // start the unity process and pass in the project to
        // open in the editor.
        var finalArgs = $"-createProject \"{projectPath}\"";
        if (args.Length > 0) {
            finalArgs += " " + string.Join(" ", args);
        }
        
        var process   = new Process() {
            StartInfo = new(exePath) {
                Arguments = finalArgs
            },
        };
        
        if (!process.Start()) {
            throw new Exception("Failed to start process");
        }
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Creating Project...", 
            async ctx => await WaitForProcess(ctx, process)
        );
    }
    
    public static async Task OpenProject(UnityPath unityPath, string projectPath) {
        await OpenProjectWithArgs(unityPath, projectPath, []);
    }
    
    public static async Task OpenProjectWithArgs(UnityPath unityPath, string projectPath, params string[] args) {
        if (!Directory.Exists(projectPath)) {
            throw new DirectoryNotFoundException(projectPath);
        }
        
        var exePath = unityPath.GetExePath();
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        // start the unity process and pass in the project to
        // open in the editor.
        var finalArgs = $"-projectPath \"{projectPath}\"";
        if (args.Length > 0) {
            finalArgs += " " + string.Join(" ", args);
        }
        var process = new Process() {
            StartInfo = new(exePath) {
                Arguments = finalArgs
            },
        };
        
        if (!process.Start()) {
            throw new Exception("Failed to start process");
        }
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Opening Project...", 
            async ctx => await WaitForProcess(ctx, process)
        );
    }
    
    static async Task WaitForProcess(StatusContext ctx, Process process) {
        while (true) {
            await Task.Delay(100);
            
            // todo: handle exit code
            if (process.HasExited) {
                ctx.Status("Process has exited!");
                AnsiConsole.MarkupLine("[green]Process exited![/]");
                break;
            }
        }
    }
}
