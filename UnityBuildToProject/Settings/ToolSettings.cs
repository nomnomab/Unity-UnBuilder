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
        data.GameSettings  = GetGameSettings(data.ProgramArgs);
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
                Console.Clear();
                
                if (o.SkipPackageAll) {
                    o.SkipPackageFetching = true;
                }
                
                var tree = new Tree("Unity Build to Project");
                tree.AddNode( "version  : 0.0.1");
                tree.AddNode($"game path: \"{o.GameExecutablePath}\"");
                
                AnsiConsole.Write(tree);
            });
            
        if (parsedArgs == null) {
            throw new Exception("Failed to parse args!");
        }
        
        return parsedArgs.Value;
    }
    
    private static GameSettings GetGameSettings(ProgramArgs args) {
        var gameName     = Path.GetFileNameWithoutExtension(args.GameExecutablePath);
        var gameSettings = GameSettings.Load(gameName);
        if (gameSettings == null) {
            var panel = new Panel(@$"It looks like this is your [underline]first time[/] trying to decompile ""{gameName}""!
A {Path.GetFileName(GameSettings.GetSavePath(gameName))}.toml was created for you next to the .exe, go ahead and modify it before running the tool again. 🙂

{GameSettings.GetSavePath(gameName)}");
            AnsiConsole.Write(panel);
            throw new Exception("Could not load GameSettings!");
        }
        
        AnsiConsole.WriteLine($"gameSettings:\n{gameSettings}");
        
        return gameSettings;
    }
}
