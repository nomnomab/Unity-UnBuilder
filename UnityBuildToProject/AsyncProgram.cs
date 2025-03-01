using CommandLine;
using Spectre.Console;

namespace Nomnom;

public static class AsyncProgram {
    public static async Task Run(AppSettings settings, string[] args) {
        LogFile.Header("Running async program");
        
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
        
        AnsiConsole.WriteLine($"args:\n - {parsedArgs.Value}");
        
        await StartConversion(parsedArgs.Value, settings);
    }
    
    /// <summary>
    /// Start the process of extracting the game build into a project.
    /// </summary>
    static async Task StartConversion(ProgramArgs args, AppSettings settings) {
        LogFile.Header("Starting conversion");
        
        var gameName     = Path.GetFileNameWithoutExtension(args.GameExecutablePath);
        var gameSettings = GameSettings.Load(gameName);
        if (gameSettings == null) {
            var panel = new Panel(@$"It looks like this is your [underline]first time[/] trying to decompile ""{gameName}""!
A {Path.GetFileName(GameSettings.GetSavePath(gameName))}.toml was created for you next to the .exe, go ahead and modify it before running the tool again. 🙂

{GameSettings.GetSavePath(gameName)}");
            AnsiConsole.Write(panel);
            return;
        }
        
        AnsiConsole.WriteLine($"gameSettings:\n{gameSettings}");
        
        var profileDuration = new ProfileDuration();
        
        var unityInstalls = UnityInstallsPath.FromFolder(settings.UnityInstallsFolder!);
        var buildPath     = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta     = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
        
        // extract assets to disk
        LogFile.Header("Extracting assets");
        
        var extractPath      = ExtractPath.FromOutputFolder("output");
        var (_, gameData, _) = Extract.ExtractGameData(settings.ExtractSettings, null, buildMeta, false);
        
        // fetch the unity install path for later
        var unityInstall     = UnityPath.FromVersion(unityInstalls, gameData.ProjectVersion.ToString());
        var extractData      = await Extract.ExtractAssets(settings, buildMeta, extractPath);
        // await Extract.DecompileShaders(buildMeta, extractPath);
        
        profileDuration.Record("Extracting Assets");
        LogFile.Header("Getting packages");
        
        // fetch the project package list for the unity version
        var packageDetection = new PackageDetection(extractData);
        var packages         = await packageDetection.GetPackagesFromVersion(unityInstall);
        
        profileDuration.Record("Getting Packages");
        
        // try to determine which packages are for this specific project
        var packageTree      = packageDetection.TryToMapPackagesToProject(packages);
        packageDetection.ApplyGameSettingsPackages(gameSettings, packageTree);
        
        packageTree.WriteToConsole();
        
        packages.WriteToDisk("unity_package.log");
        packageTree.WriteToDisk("tool_package.log");
        
        profileDuration.Record("Mapping Packages");
        LogFile.Header("Importing packages");
        
        // now import the packages
        await packageDetection.ImportPackages(unityInstall, packageTree);
        
        profileDuration.Record("Importing Packages");
        LogFile.Header("Extracting project information");
        
        // process the guids between the two projects
        var extractDb    = await GuidMapping.ExtractGuids(extractData.Config.ProjectRootPath);
        var projectDb    = await GuidMapping.ExtractGuids(extractData.GetProjectPath());
        var builtinDb    = await GuidMapping.ExtractGuids(unityInstall.GetBuiltInPackagesPath());
        
        extractDb.WriteToDisk(Path.Combine(Program.LogsFolder, "extractDb.log"));
        projectDb.WriteToDisk(Path.Combine(Program.LogsFolder, "projectDb.log"));
        builtinDb.WriteToDisk(Path.Combine(Program.LogsFolder, "builtinDb.log"));
        
        // process the types between the two projects
        var extractTypes = await RoslynUtility.ExtractTypes(extractData.Config.ProjectRootPath);
        var projectTypes = await RoslynUtility.ExtractTypes(extractData.GetProjectPath());
        var builtInTypes = await RoslynUtility.ExtractTypes(unityInstall.GetBuiltInPackagesPath());
        
        RoslynDatabase.MergeInto(
            [extractDb, projectDb, builtinDb],
            [extractTypes, projectTypes, builtInTypes]
        );
        
        // todo: process assets
        // copy over the project assets once processed
        await extractData.CopyAssetsToProject(extractData.GetProjectPath(), testFoldersOnly: false);
        ExtractData.RemoveEditorFolderFromProject(extractData.GetProjectPath());
        ExtractData.RemoveExistingPackageFoldersFromProjectScripts(gameSettings, extractData.GetProjectPath());
        ExtractData.RemoveLibraryFolders(extractData.Config.ProjectRootPath);
        
        packageTree.WriteToConsole();
        
        profileDuration.Record("Copying Assets to Project");
        LogFile.Header("Final steps");
        
        // force a recompile
        Utility.CopyOverScript(extractData.GetProjectPath(), "RecompileUnity");
        
        _ = UnityCLI.OpenProject("Opening temp project", unityInstall, false, extractData.GetProjectPath(),
            "-executeMethod Nomnom.RecompileUnity.OnLoad"
        );
        
        // await UnityCLI.OpenProject("Recompiling project", unityInstall, true, extractData.GetTempProjectPath(),
        //     "-disable-assembly-updater",
        //     "-silent-crashes",
        //     "-batchmode",
        //     "-logFile -", 
        //     "-executeMethod Nomnom.RecompileUnity.OnLoad",
        //     "-exit",
        //     "| Write-Output"
        // );
        
        // AnsiConsole.WriteLine();
        // AnsiConsole.MarkupLine("[red]Deleting[/] the temporary project folder...");
        // Directory.Delete(extractData.GetTempProjectPath(), true);
        
        // packageTree.WriteToConsole();
        
        // profileDuration.Record("Recompiled Project");
        
        LogFile.Header("Results");
        profileDuration.PrintTimestamps();
    }
}
