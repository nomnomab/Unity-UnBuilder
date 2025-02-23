using System.Diagnostics;
using Spectre.Console;

namespace Nomnom;

// https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
public static class UnityCLI {
    public static async Task CreateProject(string message, UnityPath unityPath, string projectPath) {
        await CreateProjectWithArgs(message, unityPath, projectPath, []);
    }
    
    public static async Task CreateProjectWithArgs(string message, UnityPath unityPath, string projectPath, params string[] args) {
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
            .StartAsync(message, 
            async ctx => await WaitForProcess(ctx, process)
        );
    }
    
    public static async Task OpenProject(string message, UnityPath unityPath, string projectPath) {
        await OpenProjectWithArgs(message, unityPath, projectPath, []);
    }
    
    public static async Task OpenProjectWithArgs(string message, UnityPath unityPath, string projectPath, params string[] args) {
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
            .Spinner(Spinner.Known.Aesthetic)
            .StartAsync(message, 
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
