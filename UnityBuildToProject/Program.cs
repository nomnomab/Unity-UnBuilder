using System.Reflection;
using Spectre.Console;

namespace Nomnom;

class Program {
    public static string OutputFolder {
        get {
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException(exePath);
            }
            
            var outputPath = Path.GetFullPath(
                Path.Combine(
                    exePath,
                    ".."
                )
            );
            
            return outputPath;
        }
    }
    
    static async Task Main(string[] args) {
        LoadDllsFromLib();
        
        await AsyncProgram.Run(args);
    }
    
    static void LoadDllsFromLib() {
        var libPath = Path.Join(
            OutputFolder,
            "lib"
        );
        
        Console.WriteLine("[underline]Loading dlls from /lib...[/]");
        foreach (var dll in Directory.GetFiles(libPath, "*.dll", SearchOption.TopDirectoryOnly)) {
            if (dll == null) continue;
            
            var fileName = Path.GetFileNameWithoutExtension(dll);
            if (!fileName.StartsWith("AssetRipper")) {
                continue;
            }
            
            try {
                var assembly = Assembly.LoadFrom(dll);
                AnsiConsole.MarkupLine($"[italic]Loaded {assembly.GetName().Name}[/]");
            } catch { }
        }
    }
}
