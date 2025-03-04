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
        
        var profileDuration = new ProfileDuration();
        
        // fetch unity and build information
        var unityInstalls = UnityInstallsPath.FromFolder(settings.UnityInstallsFolder!);
        var buildPath     = BuildPath.FromExe(args.GameExecutablePath);
        var buildMeta     = BuildMetadata.Parse(buildPath);
        
        AnsiConsole.WriteLine(buildMeta.ToString());
        
        // extract assets to disk
        LogFile.Header("Extracting assets");
        
        // get extraction information for AssetRipper
        var extractPath      = ExtractPath.FromOutputFolder("output");
        var (_, gameData, _) = Extract.ExtractGameData(settings.ExtractSettings, null, buildMeta, false);
        
        // fetch the unity install path for later
        var unityInstall     = UnityPath.FromVersion(unityInstalls, gameData.ProjectVersion.ToString());
        var extractData      = await Extract.ExtractAssets(args, settings, buildMeta, extractPath);
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        profileDuration.Record("Extracting assets");
        LogFile.Header("Getting packages");
        
        // fetch the project package list for the unity version
        var packageDetection = new PackageDetection(extractData);
        var newProjectPath   = await extractData.CreateNewProject(args);
        var packages         = await packageDetection.GetPackagesFromVersion(args, newProjectPath, unityInstall);
        
        profileDuration.Record("Getting packages");
        
        // try to determine which packages are for this specific project
        var packageTree      = packageDetection.TryToMapPackagesToProject(packages);
        packageDetection.ApplyGameSettingsPackages(gameSettings, packageTree);
        
        packageTree.WriteToConsole();
        
        packages.WriteToDisk("unity_package.log");
        packageTree.WriteToDisk("tool_package.log");
        
        profileDuration.Record("Mapping Packages");
        LogFile.Header("Importing packages");
        
        // now import the packages
        await packageDetection.ImportPackages(args, unityInstall, packageTree);
        profileDuration.Record("Importing packages");
        
        await ApplyFixes.FixTextMeshPro(extractData, unityInstall);
        profileDuration.Record("Fixed TextMeshPro");
        
        LogFile.Header("Extracting project information");
        
        // process the guids between the two projects
        var extractDb    = await GuidMapping.ExtractGuids(extractData.Config.ProjectRootPath);
        profileDuration.Record("Extracting guids for AssetRipper project");
        
        var projectDb    = await GuidMapping.ExtractGuids(extractData.GetProjectPath());
        profileDuration.Record("Extracting guids for final project");
        
        var builtinDb    = await GuidMapping.ExtractGuids(unityInstall.GetBuiltInPackagesPath());
        profileDuration.Record("Extracting guids for built-In packages");
        
        // process the types between the projects
        var extractTypes = await RoslynUtility.ExtractTypes(extractData.Config.ProjectRootPath);
        profileDuration.Record("Extracting types for AssetRipper project");
        
        var projectTypes = await RoslynUtility.ExtractTypes(extractData.GetProjectPath());
        profileDuration.Record("Extracting types for final project");
        
        var builtInTypes = await RoslynUtility.ExtractTypes(unityInstall.GetBuiltInPackagesPath());
        profileDuration.Record("Extracting types for built-In packages");
        
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
        profileDuration.Record("Merging guids");
        
        // copy over the project assets once processed
        await extractData.CopyAssetsToProject(extractData.GetProjectPath(), exclusionFiles, testFoldersOnly: false);
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        // tidy up the project folders
        ExtractData.RemoveEditorFolderFromProject(extractData.GetProjectPath());
        ExtractData.RemoveExistingPackageFoldersFromProjectScripts(gameSettings, extractData.GetProjectPath());
        ExtractData.RemoveLibraryFolders(extractData.Config.ProjectRootPath);
        
        packageTree.WriteToConsole();
        
        profileDuration.Record("Copying Assets to project");
        LogFile.Header("Final steps");
        
        Utility.CopyOverScript(extractData.GetProjectPath(), "RecompileUnity");
        
        // disable the compress setting
        // await UnityCLI.OpenProjectHidden("Opening project to disable compressing", unityInstall, true, extractData.GetProjectPath(),
        //     "-executeMethod Nomnom.RecompileUnity.DisableCompress"
        // );
       
        await UnityCLI.OpenProjectHidden("Opening project to recompile", unityInstall, true, extractData.GetProjectPath(),
            "-executeMethod Nomnom.RecompileUnity.OnLoad"
        );
        
        // await UnityCLI.OpenProject("Opening project to enable compressing", unityInstall, true, extractData.GetProjectPath(),
        //     "-executeMethod Nomnom.RecompileUnity.EnableCompress"
        // );
        
        profileDuration.Record("Recompiled project");
        
        // apply any appropriate fixes for the specific project
        await ApplyFixes.FixAll(settings, gameSettings, extractData, packageTree, unityInstall);
        profileDuration.Record("Initialized remaining project requirements");
        
        // final project open
        _ = UnityCLI.OpenProject("Opening project", unityInstall, false, extractData.GetProjectPath());
        
        // done!
        LogFile.Header($"Results for {gameName}");
        profileDuration.PrintTimestamps();
    }
}
