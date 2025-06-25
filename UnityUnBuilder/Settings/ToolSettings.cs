using AssetRipper.Processing;
using CommandLine;
using Spectre.Console;

namespace Nomnom;

/// <summary>
/// Holds shared settings and information that is used in a widespread manner.
/// </summary>
public record ToolSettings {
    public ProgramArgs ProgramArgs { get; private set; } = null!;
    public AppSettings AppSettings { get; private set; } = null!;
    public GameSettings GameSettings { get; private set; } = null!;
    public UnityInstallsPath UnityInstalls { get; private set; } = null!;
    public BuildPath BuildPath { get; private set; } = null!;
    public BuildMetadata BuildMetadata { get; private set; } = null!;
    
    public ExtractPath ExtractPath { get; private set; } = null!;
    public ExtractData ExtractData { get; private set; } = null!;
    public GameData GameData { get; private set; } = null!;
    
    public string GetGameName() {
        return Path.GetFileNameWithoutExtension(ProgramArgs.GameExecutablePath);
    }
    
    public UnityPath GetUnityPath(string? version = null) {
        version ??= GameData.ProjectVersion.ToString();
        return UnityPath.FromVersion(UnityInstalls, version);
    }
    
    public string GetSettingsFolder() {
        var gameName = GetGameName();
        
        // copy from settings folder
        var saveFolder = GameSettings.GetSaveFolder(Settings.FolderPath, gameName);
        
        return saveFolder;
    }
    
    public string GetSettingsProjectFolder() {
        var saveFolder    = GetSettingsFolder();
        var projectFolder = Path.Combine(saveFolder, "exclude/Resources");
        
        return projectFolder;
    }
    
    public void SetExtractPath(ExtractPath extractPath) {
        ExtractPath = extractPath;
    }
    
    public void SetExtractData(ExtractData extractData) {
        ExtractData = extractData;
    }
    
    public void SetGameData(GameData gameData) {
        GameData = gameData;
    }
    
    public static ToolSettings Create(AppSettings settings, string[] args) {
        var data = new ToolSettings();
        
        data.ProgramArgs   = GetProgramArgs(args);
        data.AppSettings   = settings;
        data.GameSettings  = GetGameSettings(data, data.ProgramArgs);
        data.UnityInstalls = UnityInstallsPath.FromFolder(data.AppSettings.UnityInstallsFolder!);
        data.BuildPath     = BuildPath.FromExe(data.ProgramArgs.GameExecutablePath);
        data.BuildMetadata = BuildMetadata.Parse(data.BuildPath);
        
        AnsiConsole.WriteLine(data.BuildMetadata.ToString());
        
        return data;
    }
    
    private static ProgramArgs GetProgramArgs(string[] args) {
        // parse the program arguments
        var parsedArgs = ProgramArgsParser.Parse(args)
            .WithParsed(o => {
                if (o.SkipPackageAll) {
                    o.SkipPackageFetching = true;
                }
                
                var tree = new Tree("Unity Build to Project");
                tree.AddNode( "version  : 0.0.1");
                tree.AddNode($"game path: \"{o.GameExecutablePath}\"");
                
                AnsiConsole.Write(tree);
            });
            
        if (parsedArgs == null || parsedArgs.Value == null) {
            throw new Exception("Failed to parse args!");
        }
        
        return parsedArgs.Value;
    }
    
    private static GameSettings GetGameSettings(ToolSettings toolSettings, ProgramArgs args) {
        if (string.IsNullOrEmpty(args.GameExecutablePath)) {
            throw new Exception("No game exe path provided!");
        }
        
        // if (string.IsNullOrEmpty(args.OutputPath)) {
        //     throw new Exception("No game output path provided!");
        // }
        
        var gameName     = Path.GetFileNameWithoutExtension(args.GameExecutablePath);
        var gameSettings = GameSettings.Load(toolSettings.GetSettingsProjectFolder(), Settings.FolderPath, gameName);
        if (gameSettings == null) {
            throw new Exception("Could not load GameSettings!");
        }
        
        if (string.IsNullOrEmpty(args.OutputPath)) {
            args.OutputPath = Path.GetFullPath(
                Path.Combine(GameSettings.GetSavePath(Settings.FolderPath, gameName), "exclude/UnityProject")
            );
        }
        
        var json = System.Text.Json.JsonSerializer.Serialize(gameSettings);
        AnsiConsole.WriteLine($"gameSettings:\n{json}");
        
        return gameSettings;
    }
}
