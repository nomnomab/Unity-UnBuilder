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
        
        var argsJson = System.Text.Json.JsonSerializer.Serialize(parsedArgs.Value);
        AnsiConsole.WriteLine($"args:\n{argsJson}");
        
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
        
        // fetch unity and build information
        var unityInstalls = UnityInstallsPath.FromFolder(settings.UnityInstallsFolder!);
        var buildPath     = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta     = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
        
        // run actual conversion process
        await RunConversion(args, settings, gameSettings, unityInstalls, buildMeta);
        
        // done!
        LogFile.Header($"Results for {gameName}");
        Profiling.TotalDuration.PrintTimestamps();
    }
    
    static async Task RunConversion(ProgramArgs args, AppSettings settings, GameSettings gameSettings, UnityInstallsPath unityInstalls, BuildMetadata buildMeta) {
        // extract assets to disk
        Profiling.Begin(
            "extract_assets",
            "Extracting assets"
        );
        
        // get extraction information for AssetRipper
        var extractPath      = ExtractPath.FromOutputFolder("output");
        var (_, gameData, _) = Extract.ExtractGameData(settings.ExtractSettings, null, buildMeta, false);
        
        // fetch the unity install path for later
        var unityInstall     = UnityPath.FromVersion(unityInstalls, gameData.ProjectVersion.ToString());
        var extractData      = await Extract.ExtractAssets(args, settings, buildMeta, extractPath);
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        await Profiling.End();
        
        Profiling.Begin(
            "getting_packages",
            "Getting package"
        );
        
        // fetch the project package list for the unity version
        var packageDetection = new PackageDetection(extractData);
        var newProjectPath   = await extractData.CreateNewProject(args);
        var packages         = await packageDetection.GetPackagesFromVersion(args, newProjectPath, unityInstall);
        
        await Profiling.End();
        
        Profiling.Begin(
            "mapping_packages",
            "Mapping Packages"
        );
        
        // try to determine which packages are for this specific project
        var packageTree      = packageDetection.TryToMapPackagesToProject(packages);
        packageDetection.ApplyGameSettingsPackages(gameSettings, packageTree);
        
        packageTree.WriteToConsole();
        
        packages.WriteToDisk("unity_package.log");
        packageTree.WriteToDisk("tool_package.log");
        
        await Profiling.End();
        
        Profiling.Begin(
            "importing_packages",
            "Importing packages"
        );
        
        // now import the packages
        await packageDetection.ImportPackages(args, unityInstall, packageTree);
        
        await Profiling.End();
        
        Profiling.Begin(
            "pre_fixes",
            "Applying pre-fixes"
        );
        
        await ApplyFixes.FixBeforeGuids(settings, gameSettings, extractData, packageTree, unityInstall);
        
        await Profiling.End();
        
        // process the guids between the projects
        var extractDb = await ExtractGuids(
            extractData.Config.ProjectRootPath,
            "extract_guids_asset_ripper",
            "Extracting guids for AssetRipper project"
        );
        
        var projectDb = await ExtractGuids(
            extractData.GetProjectPath(),
            "extract_guids_project",
            "Extracting guids for final project"
        );
        
        var builtinDb = await ExtractGuids(
            unityInstall.GetBuiltInPackagesPath(),
            "extract_guids_built_in",
            "Extracting guids for built-in packages"
        );
        
        // process the types between the projects
        var extractTypes = await ExtractTypes(
            extractData.Config.ProjectRootPath,
            "extract_types_asset_ripper",
            "Extracting types for AssetRipper project"
        );
        
        var projectTypes = await ExtractTypes(
            extractData.GetProjectPath(),
            "extract_types_project",
            "Extracting types for final project"
        );
        
        var builtInTypes = await ExtractTypes(
            unityInstall.GetBuiltInPackagesPath(),
            "extract_types_built_in",
            "Extracting types for built-in packages"
        );
        
        Profiling.Begin(
            "merging_guids",
            "Merging guids"
        );
        
        // merge all of the types into the AssetRipper project
        var merge = MergeAssets.Merge(extractData.Config.ProjectRootPath, extractDb, extractTypes);
        var guidsReplaced = RoslynDatabase.MergeInto(
            [extractDb, projectDb, builtinDb],
            [extractTypes, projectTypes, builtInTypes],
            [.. merge]
        );
        var exclusionFiles = RoslynDatabase.GetExclusionFiles(guidsReplaced, extractDb, extractTypes);
        foreach (var m in merge) {
            if (extractDb.AssociatedFilePaths.TryGetValue(m.GuidFrom, out var paths)) {
                foreach (var path in paths) {
                    exclusionFiles.Add(path);
                }
            }
        }
        
        await Profiling.End();
        
        Profiling.Begin(
            "copying_assets",
            "Copying assets to project"
        );
        
        // copy over the project assets once processed
        await extractData.CopyAssetsToProject(extractData.GetProjectPath(), exclusionFiles, testFoldersOnly: false);
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        // tidy up the project folders
        ExtractData.RemoveEditorFolderFromProject(extractData.GetProjectPath());
        ExtractData.RemoveExistingPackageFoldersFromProjectScripts(gameSettings, extractData.GetProjectPath());
        ExtractData.RemoveLibraryFolders(extractData.Config.ProjectRootPath);
        
        packageTree.WriteToConsole();
        
        await Profiling.End();
        
        Profiling.Begin(
            "recompiling_project",
            "Recompiling project"
        );
        
        LogFile.Header("Final steps");
        
        Utility.CopyOverScript(extractData.GetProjectPath(), "RecompileUnity");
       
        await UnityCLI.OpenProjectHidden("Opening project to recompile", unityInstall, true, extractData.GetProjectPath(),
            "-executeMethod Nomnom.RecompileUnity.OnLoad"
        );
        
        await Profiling.End();
        
        Profiling.Begin(
            "final_fixes",
            "Final fixes"
        );
        
        // apply any appropriate fixes for the specific project
        await ApplyFixes.FixAll(settings, gameSettings, extractData, packageTree, unityInstall);
        
        await Profiling.End();
        
        // final project open
        _ = UnityCLI.OpenProject("Opening project", unityInstall, false, extractData.GetProjectPath());
    }
    
    private static async Task<GuidDatabase> ExtractGuids(string path, string name, string message) {
        Profiling.Begin(name, message);
        var db = await GuidMapping.ExtractGuids(path);
        await Profiling.End();
        
        return db;
    }
    
    private static async Task<RoslynDatabase> ExtractTypes(string path, string name, string message) {
        Profiling.Begin(name, message);
        var types = await RoslynUtility.ExtractTypes(path);
        await Profiling.End();
        
        return types;
    }
}
