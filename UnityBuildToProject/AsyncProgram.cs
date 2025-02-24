using CommandLine;
using Spectre.Console;

namespace Nomnom;

public static class AsyncProgram {
    public static async Task Run(AppSettings settings, string[] args) {
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
        
        await StartConversion(parsedArgs.Value, settings);
    }
    
    /// <summary>
    /// Start the process of extracting the game build into a project.
    /// </summary>
    static async Task StartConversion(ProgramArgs args, AppSettings settings) {
        var unityInstalls = UnityInstallsPath.FromFolder(settings.UnityInstallsFolder!);
        var buildPath     = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta     = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
        
        // extract assets to disk
        var extractPath      = ExtractPath.FromOutputFolder("output");
        var (_, gameData, _) = Extract.ExtractGameData(settings, buildMeta);
        
        // fetch the unity install path for later
        var unityInstall     = UnityPath.FromVersion(unityInstalls, gameData.ProjectVersion.ToString());
        var extractData      = await Extract.ExtractAssets(settings, buildMeta, extractPath);
        
        // fetch the project package list for the unity version
        var packageDetection = new PackageDetection(extractData);
        var packages         = await packageDetection.GetPackagesFromVersion(unityInstall);
        
        // try to determine which packages are for this specific project
        var packageTree      = packageDetection.TryToMapPackagesToProject(packages);
        
        // now import the packages
        await packageDetection.ImportPackages(unityInstall, packageTree);
        
        // process the guids between the two projects
        var guidMapping      = new GuidMapping(extractData);
        await guidMapping.ExtractGuids();
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]Deleting[/] the temporary project folder...");
        Directory.Delete(extractData.GetTempProjectPath(), true);
        
        // UnityCLI.OpenProject(unityInstall, extractData.Config.ProjectRootPath);
    }
}
