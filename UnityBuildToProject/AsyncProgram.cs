using CommandLine;
using Spectre.Console;

namespace Nomnom;

public static class AsyncProgram {
    public static async Task Run(string[] args) {
        try {
            // parse the program arguments
            var parsedArgs = ProgramArgsParser.Parse(args)
                .WithParsed(o => {
                    Console.Clear();
                    
                    var tree = new Tree("Unity Build to Project");
                    tree.AddNode( "version  : 0.0.1");
                    tree.AddNode($"game path: \"{o.GameExecutablePath}\"");
                    
                    AnsiConsole.Write(tree);
                });
                
            if (parsedArgs == null) {
                throw new Exception("Failed to parse args!");
            }
            
            // load settings
            var settings = Settings.Load();
            if (settings == null) {
                throw new Exception("Failed to load settings");
            }
            
            await StartConversion(parsedArgs.Value);
        } catch(Exception ex) {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }
    
    /// <summary>
    /// Start the process of extracting the game build into a project.
    /// </summary>
    static async Task StartConversion(ProgramArgs args) {
        var buildPath = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
        
        // extract assets to disk
        var extractPath = ExtractPath.FromOutputFolder("output");
        var gameData    = await Extract.ExtractAssets(buildMeta, extractPath);
        
        // process assets
    }
}
