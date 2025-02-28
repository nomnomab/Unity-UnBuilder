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
    
    public static string LogsFolder {
        get {
            var path = Path.Combine(OutputFolder, "logs");
            Directory.CreateDirectory(path);
            return path;
        }
    }
    
    static async Task Main(string[] args) {
        try {
            LogFile.Create();
            LogFile.Header("Starting up");
            
            LoadDllsFromLib();
        
            // load settings
            var settings = AppSettings.Load();
            if (settings == null) {
                var panel = new Panel(@$"It looks like this is your [underline]first time[/] running this tool!
    A settings.toml was created for you next to the .exe, go ahead and modify it before running the tool again. 🙂

    {AppSettings.SavePath}");
                AnsiConsole.Write(panel);
                return;
            }
            
            AppSettings.Validate(settings);
            AnsiConsole.WriteLine($"settings:\n{settings}");
            
            await AsyncProgram.Run(settings, args);
        } catch(Exception ex) {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
        } finally {
            LogFile.Close();
        }
    }
    
    static void LoadDllsFromLib() {
        var libPath = Path.Join(
            OutputFolder,
            "lib"
        );
        
        AnsiConsole.WriteLine("Loading dlls from /lib...");
        foreach (var dll in Directory.GetFiles(libPath, "*.dll", SearchOption.TopDirectoryOnly)) {
            if (dll == null) continue;
            
            var fileName = Path.GetFileNameWithoutExtension(dll);
            if (!fileName.StartsWith("AssetRipper")) {
                continue;
            }
            
            try {
                var assembly = Assembly.LoadFrom(dll);
                AnsiConsole.WriteLine($"Loaded {assembly.GetName().Name}");
            } catch { }
        }
    }
}
