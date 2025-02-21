using CommandLine;
using Spectre.Console;

namespace Nomnom;

class Program {
    static async Task Main(string[] args) {
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
