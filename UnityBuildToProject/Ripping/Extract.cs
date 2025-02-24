using AssetRipper.Export.UnityProjects;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Logging;
using AssetRipper.Processing;
using Spectre.Console;

namespace Nomnom;

public static class Extract {
    public static (LibraryConfiguration, GameData, ExportHandler) ExtractGameData(AppSettings settings, BuildMetadata buildMetadata) {
        var config = new LibraryConfiguration();
        config.LoadFromDefaultPath();
        
        if (settings.ExtractSettings != null) {
            config.ImportSettings.ScriptContentLevel             = settings.ExtractSettings.ScriptContentLevel;
            config.ImportSettings.StreamingAssetsMode            = settings.ExtractSettings.StreamingAssetsMode;
            
            config.ExportSettings.AudioExportFormat              = settings.ExtractSettings.AudioExportFormat;
            config.ExportSettings.TextExportMode                 = settings.ExtractSettings.TextExportMode;
            config.ExportSettings.SpriteExportMode               = settings.ExtractSettings.SpriteExportMode;
            config.ExportSettings.ShaderExportMode               = settings.ExtractSettings.ShaderExportMode;
            config.ExportSettings.ScriptLanguageVersion          = settings.ExtractSettings.ScriptLanguageVersion;
            config.ExportSettings.ScriptExportMode               = settings.ExtractSettings.ScriptExportMode;
            config.ExportSettings.LightmapTextureExportFormat    = settings.ExtractSettings.LightmapTextureExportFormat;
            config.ExportSettings.ImageExportFormat              = settings.ExtractSettings.ImageExportFormat;
            
            config.ProcessingSettings.EnablePrefabOutlining      = settings.ExtractSettings.EnablePrefabOutlining;
            config.ProcessingSettings.EnableStaticMeshSeparation = settings.ExtractSettings.EnableStaticMeshSeparation;
            config.ProcessingSettings.EnableAssetDeduplication   = settings.ExtractSettings.EnableAssetDeduplication;
            config.ProcessingSettings.BundledAssetsExportMode    = settings.ExtractSettings.BundledAssetsExportMode;
        }
        
        var exportHandler = new ExportHandler(config);
        var inputPath     = buildMetadata.Path.exePath;
        var gameData      = exportHandler.LoadAndProcess([
            inputPath
        ]);
        
        return (config, gameData, exportHandler);
    }
    
    public static async Task<ExtractData> ExtractAssets(AppSettings settings, BuildMetadata buildMetadata, ExtractPath extractPath) {
        AnsiConsole.MarkupLine("[underline]Extracting assets...[/]");
        
        // log with the specific one below
        Logger.Clear();
        Logger.Add(new ExtractLogger());
        
        var (config, gameData, exportHandler) = ExtractGameData(settings, buildMetadata);
        
        
        PrintLibraryConfiguration(config, false);
        
        await Task.Delay(1000);
        
        // starts a thread to export with AssetRipper
        // and waits for it to finish or fail
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Aesthetic)
            .StartAsync("Exporting...", 
            async ctx => await WaitForAssetRipper(ctx, extractPath, exportHandler, gameData)
        );
        
        Logger.Clear();
        PrintLibraryConfiguration(config, false);
        
        // this holds the information about the build folder
        return new ExtractData() {
            GameData = gameData,
            Config   = config
        };
    }
    
    static async Task WaitForAssetRipper(StatusContext ctx, ExtractPath extractPath, ExportHandler exportHandler, GameData gameData) {
        var task = Task.Run(() => {
            var outputPath = extractPath.folderPath;
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
                ctx.Status("Exporting...");
                continue;
            }
            
            ctx.Status("Export faulted.");
            AnsiConsole.MarkupLine("[red]Exported faulted![/]");
            throw new Exception("Export faulted.");
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
            new($"Bundled Assets Export Mode: {config.ProcessingSettings.BundledAssetsExportMode}"),
            new($"Script Level              : {config.ImportSettings.ScriptContentLevel}"),
            new($"Ignore Streaming Assets   : {config.ImportSettings.IgnoreStreamingAssets}"),
            new($"Streaming Assets Mode     : {config.ImportSettings.StreamingAssetsMode}"),
            new($"Audio Export Format       : {config.ExportSettings.AudioExportFormat}"),
            new($"Text Export Mode          : {config.ExportSettings.TextExportMode}"),
            new($"Sprite Export Mode        : {config.ExportSettings.SpriteExportMode}"),
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
        
        AnsiConsole.MarkupLine($"[gray]{category}[/]: [{color}]{message}[/]");
    }
}
