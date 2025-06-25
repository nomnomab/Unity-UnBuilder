using System.Diagnostics;
using Spectre.Console;

namespace Nomnom;

// https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
public static class UnityCLI {
    public static async Task CreateProject(string message, UnityPath unityPath, bool routeStd, string projectPath) {
        await CreateProjectWithArgs(message, unityPath, projectPath, routeStd, []);
    }
    
    public static async Task CreateProject(string message, UnityPath unityPath, bool routeStd, string projectPath, params string[] args) {
        await CreateProjectWithArgs(message, unityPath, projectPath, routeStd, args);
    }
    
    public static async Task CreateProjectWithArgs(string message, UnityPath unityPath, string projectPath, bool routeStd, params string[] args) {
        if (!Directory.Exists(projectPath)) {
            throw new DirectoryNotFoundException(projectPath);
        }
        
        var exePath = unityPath.GetExePath();
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        await RunProcess(unityPath, message, routeStd, [
            $"-createProject \"{projectPath}\"",
            ..args,
        ]);
    }
    
    public static async Task OpenProjectHiddenNoQuit(string message, UnityPath unityPath, bool routeStd, string projectPath, params string[] args) {
        await OpenProjectWithArgs(message, unityPath, projectPath, routeStd, [
            "-disable-assembly-updater",
            "-silent-crashes",
            "-batchmode",
            "-nographics",
            "-logFile -", 
            .. args,
            "| Write-Output"
        ]);
    }
    
    public static async Task OpenProjectHidden(string message, UnityPath unityPath, bool routeStd, string projectPath, params string[] args) {
        await OpenProjectWithArgs(message, unityPath, projectPath, routeStd, [
            "-disable-assembly-updater",
            // "-silent-crashes",
            "-batchmode",
            "-nographics",
            "-logFile -", 
            .. args,
            "-quit",
            "| Write-Output"
        ]);
    }
    
    public static async Task OpenProject(string message, UnityPath unityPath, bool routeStd, string projectPath) {
        await OpenProjectWithArgs(message, unityPath, projectPath, routeStd, []);
    }
    
    public static async Task OpenProject(string message, UnityPath unityPath, bool routeStd, string projectPath, params string[] args) {
        await OpenProjectWithArgs(message, unityPath, projectPath, routeStd, args);
    }
    
    public static async Task OpenProjectWithArgs(string message, UnityPath unityPath, string projectPath, bool routeStd, params string[] args) {
        if (!Directory.Exists(projectPath)) {
            throw new DirectoryNotFoundException(projectPath);
        }
        
        // start the unity process and pass in the project to
        // open in the editor.
        await RunProcess(unityPath, message, routeStd, [
            $"-projectPath \"{projectPath}\"",
            ..args
        ]);
    }
    
    public static async Task OpenProjectScene(string message, UnityPath unityPath, bool routeStd, string scenePath, params string[] args) {
        if (!File.Exists(scenePath)) {
            throw new FileNotFoundException(scenePath);
        }
        
        // start the unity process and pass in the scene to
        // open in the editor.
        await RunProcess(unityPath, message, routeStd, [
            $"-openfile \"{scenePath}\"",
            ..args
        ]);
    }
    
    static async Task RunProcess(UnityPath unityPath, string message, bool routeStd, params string[] args) {
        var exePath = unityPath.GetExePath()
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        var finalArgs = string.Empty;
        if (args.Length > 0) {
            var argList = string.Join(" ", args);
            finalArgs += argList;
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"Running process with arguments:\n{finalArgs}");
        AnsiConsole.WriteLine();
        
        var process = new Process() {
            StartInfo = new(exePath) {
                Arguments       = finalArgs,
            },
        };
        
        const string PREFIX = "from_patcher::";
        if (routeStd) {
            process.StartInfo.RedirectStandardError  = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute        = false;
            
            process.OutputDataReceived += (a, b) => {
                var data = b.Data;
                if (data != null) {
                    try {
                        if (data.StartsWith(PREFIX)) {
                            AnsiConsole.MarkupLine(data[PREFIX.Length..]);
                        } else {
                            AnsiConsole.MarkupLineInterpolated($"[grey]Info[/]: {data}");
                        }
                    } catch {
                        AnsiConsole.WriteLine(data);
                    }
                }
            };
            
            process.ErrorDataReceived += (a, b) => {
                var data = b.Data;
                if (data != null) {
                    AnsiConsole.Markup("[red]ERROR[/]: ");
                    if (data.StartsWith(PREFIX)) {
                        try {
                            AnsiConsole.MarkupLine(data[PREFIX.Length..]);
                        } catch {
                            AnsiConsole.WriteLine(data);
                        }
                    } else {
                        AnsiConsole.WriteLine(data);
                    }
                }
            };
        }
        
        if (!process.Start()) {
            throw new Exception("Failed to start process");
        }
        
        if (routeStd) {
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .StartAsync(message.Replace('[', '_').Replace(']', '_'), 
            async ctx => await WaitForProcess(ctx, process)
        );
        
        // finish
        AnsiConsole.MarkupLine($"Exit Code: {process.ExitCode}");
        
        if (process.ExitCode != 0) {
            throw new Exception($"ExitCode was {process.ExitCode}. Please refer to the log files to find the error!\n\n{Paths.LogsFolder}");
        }
        
        await Task.Delay(500);
    }
    
    static async Task WaitForProcess(StatusContext ctx, Process process) {
        while (true) {
            await Task.Delay(100);
            
            if (process.HasExited) {
                ctx.Status("Process has exited!");
                AnsiConsole.MarkupLine("\n[green]Process exited![/]\n");
                break;
            }
        }
    }
}
