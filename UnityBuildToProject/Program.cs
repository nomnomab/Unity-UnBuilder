using CommandLine;
using Spectre.Console;

namespace Nomnom;

class Program {
    static void Main(string[] args) {
        // parse the program arguments
        ProgramArgsParser.Parse(args)
            .WithParsed(o => {
                Console.Clear();
                
                var tree = new Tree("Unity Build to Project");
                tree.AddNode( "version  : 0.0.1");
                tree.AddNode($"game path: \"{o.GameExecutablePath}\"");
                
                AnsiConsole.Write(tree);
                
                try {
                    StartConversion(o);
                } catch(Exception ex) {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                }
            });
    }
    
    /// <summary>
    /// Start the process of extracting the game build into a project.
    /// </summary>
    static void StartConversion(ProgramArgs args) {
        var buildPath = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
    }
}
