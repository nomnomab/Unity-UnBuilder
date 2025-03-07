using CommandLine;
using Spectre.Console;

namespace Nomnom;

public static class AsyncProgram {
    public static async Task Run(AppSettings settings, string[] args) {
        LogFile.Header("Running async program");
        
        var toolData = ToolSettings.Create(settings, args);
        
        var argsJson = System.Text.Json.JsonSerializer.Serialize(toolData.ProgramArgs);
        AnsiConsole.WriteLine($"args:\n{argsJson}");
        
        await StartConversion(toolData);
    }
    
    /// <summary>
    /// Start the process of extracting the game build into a project.
    /// </summary>
    static async Task StartConversion(ToolSettings settings) {
        LogFile.Header("Starting conversion");
        
        // run actual conversion process
        await RunConversion(settings);
        
        // done!
        LogFile.Header($"Results for {settings.GetGameName()}");
        Profiling.TotalDuration.PrintTimestamps();
    }
    
    static async Task RunConversion(ToolSettings settings) {
        // extract assets to disk
        Profiling.Begin(
            "extract_assets",
            "Extracting assets"
        );
        
        // get extraction information for AssetRipper
        var gameName         = settings.GetGameName();
        var extractPath      = ExtractPath.FromOutputFolder($"output_{gameName}");
        var (_, gameData, _) = Extract.ExtractGameData(settings, null, false);
        settings.SetExtractPath(extractPath);
        settings.SetGameData(gameData);
        
        // fetch the unity install path for later
        var extractData      = await Extract.ExtractAssets(settings);
        settings.SetExtractData(extractData);
        
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        // ApplyFixes.FixMissingGuids(gameSettings, extractData);
        
        await Profiling.End();
        
        // fetch the project package list for the unity version
        Profiling.Begin(
            "getting_packages",
            "Getting package"
        );
        
        var packageDetection = new PackageDetection(extractData);
        var newProjectPath   = await extractData.CreateNewProject(settings.ProgramArgs);
        
        var versionGT2018    = gameData.ProjectVersion.GreaterThanOrEquals(2018);
        Console.WriteLine($"{gameData.ProjectVersion} >= 2018? {versionGT2018}");
        
        PackageTree? packageTree = null;
        if (versionGT2018) {
            var packages = versionGT2018 ? await packageDetection.GetPackagesFromVersion(settings, newProjectPath) : new UnityPackages() {
                Packages = []
            };
            
            await Profiling.End();
            
            Profiling.Begin(
                "mapping_packages",
                "Mapping Packages"
            );
            
            // try to determine which packages are for this specific project
            // todo: handle 2017 not supporting this :))))))
            
            packageTree = packageDetection.TryToMapPackagesToProject(packages);
            packageDetection.ApplyGameSettingsPackages(settings.GameSettings, packageTree);
            
            packageTree.WriteToConsole();
            
            packages.WriteToDisk("unity_package.log");
            packageTree.WriteToDisk("tool_package.log");
            
            await Profiling.End();
            
            Profiling.Begin(
                "importing_packages",
                "Importing packages"
            );
            
            // now import the packages
            await packageDetection.ImportPackages(settings, packageTree);
            
            await Profiling.End();
        }
        
        Profiling.Begin(
            "pre_fixes",
            "Applying pre-fixes"
        );
        
        await ApplyFixes.FixBeforeGuids(settings);
        
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
        
        var unityPath = settings.GetUnityPath();
        var builtinDb = versionGT2018 ? await ExtractGuids(
            unityPath.GetBuiltInPackagesPath(),
            "extract_guids_built_in",
            "Extracting guids for built-in packages"
        ) : new GuidDatabase() {
            Assets              = [],
            AssociatedFilePaths = [],
            FilePathToGuid      = [],
        };
        
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
        
        var builtInTypes = versionGT2018 ? await ExtractTypes(
            unityPath.GetBuiltInPackagesPath(),
            "extract_types_built_in",
            "Extracting types for built-in packages"
        ) : new RoslynDatabase() {
            FullNameToFilePath    = [],
            ShaderNameToFilePaths = []
        };
        
        Profiling.Begin(
            "merging_guids",
            "Merging guids"
        );
        
        // merge all of the types into the AssetRipper project
        var merge = MergeAssets.Merge(extractDb, extractTypes);
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
        ExtractData.RemoveExistingPackageFoldersFromProjectScripts(settings.GameSettings, settings.ExtractData.GetProjectPath());
        ExtractData.RemoveLibraryFolders(extractData.Config.ProjectRootPath);
        
        await Profiling.End();
        
        Profiling.Begin(
            "recompiling_project",
            "Recompiling project"
        );
        
        LogFile.Header("Final steps");
        
        ApplyFixes.FixBeforeRecompile(settings);
        
        Utility.CopyOverScript(extractData.GetProjectPath(), "RecompileUnity");
       
        await UnityCLI.OpenProjectHidden("Opening project to recompile", unityPath, true, extractData.GetProjectPath(),
            "-executeMethod Nomnom.RecompileUnity.OnLoad"
        );
        
        await Profiling.End();
        
        Profiling.Begin(
            "final_fixes",
            "Final fixes"
        );
        
        // apply any appropriate fixes for the specific project
        await ApplyFixes.FixAfterRecompile(settings, packageTree);
        
        await Profiling.End();
        
        // final project open
        _ = UnityCLI.OpenProject("Opening project", unityPath, false, extractData.GetProjectPath());
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
