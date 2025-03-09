using AssetRipper.Export.UnityProjects;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.Processing;
using Spectre.Console;

namespace Nomnom;

public static class Extract {
    public static (LibraryConfiguration, GameData, ExportHandler) ExtractGameData(ToolSettings settings, string? customPath, bool process) {
        return ExtractGameData(
            settings.AppSettings.ExtractSettings,
            settings.BuildMetadata,
            customPath,
            process
        );
    }
    
    public static (LibraryConfiguration, GameData, ExportHandler) ExtractGameData(ExtractSettings? extractSettings, BuildMetadata buildMetadata, string? customPath, bool process) {
        var config = new LibraryConfiguration();
        config.LoadFromDefaultPath();
        
        if (customPath != null) {
            config.ExportRootPath = customPath;
        }
        
        if (extractSettings != null) {
            config.ImportSettings.ScriptContentLevel             = extractSettings.ScriptContentLevel;
            config.ImportSettings.StreamingAssetsMode            = extractSettings.StreamingAssetsMode;
            
            config.ExportSettings.AudioExportFormat              = extractSettings.AudioExportFormat;
            config.ExportSettings.TextExportMode                 = extractSettings.TextExportMode;
            config.ExportSettings.SpriteExportMode               = extractSettings.SpriteExportMode;
            config.ExportSettings.ShaderExportMode               = extractSettings.ShaderExportMode;
            config.ExportSettings.ScriptLanguageVersion          = extractSettings.ScriptLanguageVersion;
            config.ExportSettings.ScriptExportMode               = extractSettings.ScriptExportMode;
            config.ExportSettings.LightmapTextureExportFormat    = extractSettings.LightmapTextureExportFormat;
            config.ExportSettings.ImageExportFormat              = extractSettings.ImageExportFormat;
            
            config.ProcessingSettings.EnablePrefabOutlining      = extractSettings.EnablePrefabOutlining;
            config.ProcessingSettings.EnableStaticMeshSeparation = extractSettings.EnableStaticMeshSeparation;
            config.ProcessingSettings.EnableAssetDeduplication   = extractSettings.EnableAssetDeduplication;
            // config.ProcessingSettings.BundledAssetsExportMode    = extractSettings.BundledAssetsExportMode;
        }
        
        var exportHandler = new ExportHandler(config);
        var inputPath     = buildMetadata.Path.exePath;
        var gameData      = process ? exportHandler.LoadAndProcess([
            inputPath
        ]) : exportHandler.Load([
            inputPath
        ]);
        
        return (config, gameData, exportHandler);
    }
    
    public static async Task<ExtractData> ExtractAssets(ToolSettings settings) {
        if(!settings.ProgramArgs.SkipAssetRipper) {
            AnsiConsole.MarkupLine("[underline]Extracting assets...[/]");
            
            // log with the specific one below
            Logger.Clear();
            Logger.Add(new ExtractLogger());
        }
        
        string? shadersRoot = null;
        if(!settings.ProgramArgs.SkipAssetRipper) {
            // extract the dummy shaders
            // this also extracts the models since I have no way to disable it
            shadersRoot = await ExtractDecompiledShaders(settings);
            
            await Task.Delay(1000);
        }
        
        // extract the normal content
        var extractData = await ExtractAll(settings);
        
        Logger.Clear();
        
        if (shadersRoot != null) {
            // copy over shaders that have a meta file associated!
            var fromPath = Path.Combine(shadersRoot, "ExportedProject", "Assets");
            var toPath   = Path.Combine(extractData.Config.ProjectRootPath, "Assets");
            var shaders  = Directory.GetFiles(fromPath, "*.shader", SearchOption.AllDirectories);
            foreach (var shader in shaders) {
                var newPath = shader.Replace(fromPath, toPath);
                var dir     = Path.GetDirectoryName(newPath);
                Directory.CreateDirectory(dir!);
                
                File.Copy(shader, newPath, true);
                
                AnsiConsole.WriteLine($"Copied:\nfrom:{Utility.ClampPathFolders(shader, 6)}\nto: {Utility.ClampPathFolders(newPath, 6)}");
            }
            
            // remove the shaders root
            await Paths.DeleteDirectory(shadersRoot);
        }
        
        // this holds the information about the build folder
        return extractData;
    }
    
    private static async Task<ExtractData> ExtractAll(ToolSettings settings) {
        var (config, gameData, exportHandler) = ExtractGameData(settings, null, true);
        var extractData = new ExtractData() {
            GameName     = settings.BuildMetadata.GetName(),
            OutputFolder = settings.ProgramArgs.OutputPath,
            GameData     = gameData,
            Config       = config
        };
        
        await Task.Delay(1000);
        
        if(!settings.ProgramArgs.SkipAssetRipper) {
            PrintLibraryConfiguration(config, false);
            
            // starts a thread to export with AssetRipper
            // and waits for it to finish or fail
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Aesthetic)
                .StartAsync("Exporting...", 
                async ctx => await WaitForAssetRipper(ctx, "Exporting...", settings.ExtractPath.folderPath, exportHandler, gameData)
            );
        } else {
            extractData.Config.ExportRootPath = settings.ExtractPath.folderPath;
            
            if (!Directory.Exists(extractData.Config.ProjectRootPath)) {
                AnsiConsole.MarkupLine($"[red]Error[/]: No AssetRipper project found. Make sure you run without --skip_ar at least once.");
                throw new DirectoryNotFoundException(extractData.Config.ProjectRootPath);
            }
        }
        
        PrintLibraryConfiguration(config, false);
        return extractData;
    }
    
    private static async Task<string> ExtractDecompiledShaders(ToolSettings settings) {
        var (config, gameData, exportHandler) = ExtractGameData(settings, null, true);
        
        var shadersOutputRoot       = settings.ExtractPath.folderPath + "_shaders";
        var decompiledShadersConfig = new LibraryConfiguration {
            ExportSettings = new() {
                ScriptExportMode            = ScriptExportMode.DllExportWithoutRenaming,
                ShaderExportMode            = ShaderExportMode.Decompile,
                AudioExportFormat           = AudioExportFormat.Yaml,
                TextExportMode              = TextExportMode.Bytes,
                SpriteExportMode            = SpriteExportMode.Yaml,
                LightmapTextureExportFormat = LightmapTextureExportFormat.Yaml,
            },
            ImportSettings = new() {
                ScriptContentLevel    = AssetRipper.Import.Configuration.ScriptContentLevel.Level0,
                StreamingAssetsMode   = AssetRipper.Import.Configuration.StreamingAssetsMode.Ignore,
                IgnoreStreamingAssets = true,
            },
            ProcessingSettings = new() {
                EnableAssetDeduplication   = false,
                EnablePrefabOutlining      = false,
                EnableStaticMeshSeparation = false,
            },
        };
        var extractData = new ExtractData() {
            GameName     = settings.BuildMetadata.GetName(),
            OutputFolder = settings.ProgramArgs.OutputPath,
            GameData     = gameData,
            Config       = decompiledShadersConfig
        };
        
        await Task.Delay(1000);
        
        if(!settings.ProgramArgs.SkipAssetRipper) {
            PrintLibraryConfiguration(decompiledShadersConfig, false);
            
            // starts a thread to export with AssetRipper
            // and waits for it to finish or fail
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Aesthetic)
                .StartAsync("Decompiling shaders...", 
                async ctx => await WaitForAssetRipper(ctx, "Decompiling shaders...", shadersOutputRoot, exportHandler, gameData)
            );
            
            await Task.Delay(1000);
        } else {
            extractData.Config.ExportRootPath = settings.ExtractPath.folderPath;
        }
        
        // move the shaders outside
        var rootAbove = Path.Combine(shadersOutputRoot, "..", "decompiled_shaders");
        var shaders  = Directory.GetFiles(shadersOutputRoot, "*.shader", SearchOption.AllDirectories);
        foreach (var shader in shaders) {
            // if a shader file properly decompiled, it will have a meta file
            var metaFile = shader + ".meta";
            if (!File.Exists(metaFile)) {
                continue;
            }
            
            var newPath = shader.Replace(shadersOutputRoot, rootAbove);
            var dir     = Path.GetDirectoryName(newPath);
            Directory.CreateDirectory(dir!);
            
            File.Copy(shader, newPath, true);
            
            AnsiConsole.WriteLine($"Copied:\nfrom:{Utility.ClampPathFolders(shader, 6)}\nto: {Utility.ClampPathFolders(newPath, 6)}");
        }
        
        await Paths.DeleteDirectory(shadersOutputRoot);
        
        return rootAbove;
    }
    
    static async Task WaitForAssetRipper(StatusContext ctx, string message, string outputPath, ExportHandler exportHandler, GameData gameData) {
        var task = Task.Run(async () => {
            // remove the previous rip
            var dllsPath = Path.Combine(outputPath, "AuxiliaryFiles");
            if (Directory.Exists(dllsPath)) {
                await Paths.DeleteDirectory(dllsPath);
            }
            
            var exportPath = Path.Combine(outputPath, "ExportedProject");
            if (Directory.Exists(exportPath)) {
                await Paths.DeleteDirectory(exportPath);
            }
            
            exportHandler.Export(gameData, outputPath);
        });
        
        while (true) {
            await Task.Delay(100);
            
            if (task.IsCompletedSuccessfully) {
                ctx.Status("Completed export!");
                AnsiConsole.MarkupLine("[green]Exported assets![/]");
                break;
            }
            
            if (!task.IsCompleted) {
                ctx.Status(message);
                continue;
            }
            
            if (task.IsFaulted) {
                ctx.Status("Export faulted.");
                AnsiConsole.MarkupLine("[red]Exported faulted![/]");
                
                if (task.Exception != null) {
                    throw task.Exception;
                } else {
                    throw new Exception("Export faulted.");
                }
            }
            
            if (task.Exception != null) {
                ctx.Status("Export failed.");
                AnsiConsole.MarkupLine("[red]Exported failed![/]");
                throw task.Exception;
            }
            
            break;
        }
    }
    
    static void PrintLibraryConfiguration(LibraryConfiguration config, bool hasPremium) {
        var rows = new List<Text>
        {
            new($"Platform                  : {config.Platform}"),
            new($"Version                   : {config.Version}"),
            new($"Assets Path               : \"{config.AssetsPath}\""),
            new($"Export Root Path          : \"{config.ExportRootPath}\""),
            new($"Project Root Path         : \"{config.ProjectRootPath}\""),
            new($"Auxiliary Files Path      : \"{config.AuxiliaryFilesPath}\""),
            new($"Project Settings Path     : \"{config.ProjectSettingsPath}\""),
            new($"Disable Script Import     : {config.DisableScriptImport}"),
            // new($"Bundled Assets Export Mode: {config.ProcessingSettings.BundledAssetsExportMode}"),
            new($"Script Level              : {config.ImportSettings.ScriptContentLevel}"),
            new($"Ignore Streaming Assets   : {config.ImportSettings.IgnoreStreamingAssets}"),
            new($"Streaming Assets Mode     : {config.ImportSettings.StreamingAssetsMode}"),
            new($"Audio Export Format       : {config.ExportSettings.AudioExportFormat}"),
            new($"Text Export Mode          : {config.ExportSettings.TextExportMode}"),
            new($"Sprite Export Mode        : {config.ExportSettings.SpriteExportMode}"),
            new($"Shader Export Mode        : {config.ExportSettings.ShaderExportMode}"),
            new($"Script Language Version   : {config.ExportSettings.ScriptLanguageVersion}"),
            new($"Script Export Mode        : {config.ExportSettings.ScriptExportMode}"),
            new($"Lightmap Export Format    : {config.ExportSettings.LightmapTextureExportFormat}"),
            new($"Image Export Format       : {config.ExportSettings.ImageExportFormat}")
        };
        
        if (hasPremium) {
            // premium
            rows.Add(new Text("[Premium]", new Style(Color.Yellow)));
            rows.Add(new Text($"Prefab Outlining      : {config.ProcessingSettings.EnablePrefabOutlining}"));
            rows.Add(new Text($"Static Mesh Separation: {config.ProcessingSettings.EnableStaticMeshSeparation}"));
            rows.Add(new Text($"Asset Deduplication   : {config.ProcessingSettings.EnableAssetDeduplication}"));
        }
        
        var panel = new Panel(new Rows(rows)) {
            Header = new PanelHeader("AssetRipper Configuration")
        };
        AnsiConsole.Write(panel);
    }
}

class ExtractLogger : ILogger {
    public void BlankLine(int numLines) {
        for (int i = 0; i < numLines; i++) {}
        AnsiConsole.WriteLine();
    }

    public void Log(LogType type, LogCategory category, string message) {
        var color = type switch {
            LogType.Info => "white",
            LogType.Warning => "yellow",
            LogType.Error => "red",
            LogType.Verbose => "white",
            LogType.Debug => "gray",
            _ => throw new NotImplementedException(),
        };
        
        try {
            AnsiConsole.MarkupLine($"[gray]{category}[/]: [{color}]{message}[/]");
        } catch {
            Console.WriteLine($"{category}: {message}");
        }
    }
}
