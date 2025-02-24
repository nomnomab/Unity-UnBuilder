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
        try {
            LoadDllsFromLib();
        
            // load settings
            var settings = AppSettings.Load();
            if (settings == null) {
                var panel = new Panel(@$"Looks like your [underline]first time[/] running this tool!
    A settings.toml was created for you next to the .exe, go ahead and modify it before running the tool again. 🙂

    {AppSettings.SavePath}");
                AnsiConsole.Write(panel);
                return;
            }
            
            AppSettings.Validate(settings);
            
            await AsyncProgram.Run(settings, args);
        } catch(Exception ex) {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
        }
    }
    
    static void LoadDllsFromLib() {
        var libPath = Path.Join(
            OutputFolder,
            "lib"
        );
        
        AnsiConsole.MarkupLine("[underline]Loading dlls from /lib...[/]");
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
