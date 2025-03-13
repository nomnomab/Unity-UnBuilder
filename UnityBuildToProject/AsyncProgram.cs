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
        
        // pre-check the unity installation
        _ = settings.GetUnityPath();
        
        // fetch the unity install path for later
        var extractData = await Extract.ExtractAssets(settings);
        var projectPath = extractData.GetProjectPath();
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
            
            packageTree = packageDetection.TryToMapPackagesToProject(settings, packages);
            packageDetection.ApplyGameSettingsPackages(settings, packageTree);
            
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
            settings,
            extractData.Config.ProjectRootPath,
            "extract_guids_asset_ripper",
            "Extracting guids for AssetRipper project"
        );
        
        var projectDb = await ExtractGuids(
            settings,
            projectPath,
            "extract_guids_project",
            "Extracting guids for final project"
        );
        
        var unityPath = settings.GetUnityPath();
        var builtinDb = versionGT2018 ? await ExtractGuids(
            settings,
            unityPath.GetBuiltInPackagesPath(),
            "extract_guids_built_in",
            "Extracting guids for built-in packages"
        ) : new GuidDatabase() {
            Assets              = [],
            AssociatedFilePaths = [],
            FilePathToGuid      = [],
            DllReferences       = [],
        };
        
        // process the types between the projects
        var extractTypes = await ExtractTypes(
            extractData.Config.ProjectRootPath,
            "extract_types_asset_ripper",
            "Extracting types for AssetRipper project"
        );
        
        var projectTypes = await ExtractTypes(
            projectPath,
            "extract_types_project",
            "Extracting types for final project"
        );
        
        await GuidMapping.ExtractDllGuids(settings, projectDb, projectTypes, projectPath);
        
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
        var merge = MergeAssets.Merge(extractDb, extractTypes).ToList();
        
        // merge guids to any dll refs
        foreach (var dll in projectDb.DllReferences) {
            // find the type from the first typedb that maps to the one in the dll
            // then replace with the guid from the dll + the fileId from the ref
            if (extractTypes.FullNameToFilePath.TryGetValue(dll.TypeName, out var filePath)) {
                if (extractDb.FilePathToGuid.TryGetValue(filePath, out var guid)) {
                    // this is the guid that needs to be replaced
                    merge.Add(new GuidDatabaseMerge(
                        guid,
                        dll.Ref.Guid,
                        dll.Ref.FileId,
                        dll.Ref.Type
                    ));
                }
            }
        }
        
        var guidsReplaced = RoslynDatabase.MergeInto(
            [extractDb, projectDb, builtinDb],
            [extractTypes, projectTypes, builtInTypes],
            [.. merge]
        );
        var (excludeFiles, excludeDirs) = RoslynDatabase.GetExclusionFiles(settings, guidsReplaced, extractDb, extractTypes);
        foreach (var m in merge) {
            if (extractDb.AssociatedFilePaths.TryGetValue(m.GuidFrom, out var paths)) {
                foreach (var path in paths) {
                    excludeFiles.Add(path);
                }
            }
        }
        
        await Profiling.End();
        
        Profiling.Begin(
            "copying_assets",
            "Copying assets to project"
        );
        
        // copy over the project assets once processed
        await extractData.CopyAssetsToProject(settings, projectPath, excludeFiles, excludeDirs, extractDb, testFoldersOnly: false);
        RoslynDatabase.RemoveAllNewFiles(extractData.Config.ProjectRootPath);
        
        // tidy up the project folders
        ExtractData.RemoveEditorFolderFromProject(projectPath);
        ExtractData.RemoveExistingPackageFoldersFromProjectScripts(settings, projectPath);
        ExtractData.RemoveLibraryFolders(extractData.Config.ProjectRootPath);
        
        await Profiling.End();
        
        Profiling.Begin(
            "recompiling_project",
            "Recompiling project"
        );
        
        LogFile.Header("Final steps");
        
        ApplyFixes.FixBeforeRecompile(settings, packageTree);
        
        Utility.CopyOverScript(projectPath, "RecompileUnity");
       
        await UnityCLI.OpenProjectHidden("Opening project to recompile", unityPath, true, projectPath,
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
        if (settings.GameSettings.General.OpenScenePath is {} scenePath) {
            scenePath = Path.Combine(projectPath, "Assets", scenePath);
            if (!scenePath.EndsWith(".unity")) {
                scenePath += ".unity";
            }
            
            if (!File.Exists(scenePath)) {
                AnsiConsole.MarkupLine($"[yellow]No scene found[/] at \"{scenePath}\"");
                _ = UnityCLI.OpenProject("Opening project", unityPath, false, projectPath);
            } else {
                AnsiConsole.MarkupLine($"Opening scene at \"{scenePath}\"");
                _ = UnityCLI.OpenProjectScene($"Opening project with scene \"{scenePath}\"", unityPath, false, scenePath);
            }
        } else {
            _ = UnityCLI.OpenProject("Opening project", unityPath, false, projectPath);
        }
    }
    
    private static async Task<GuidDatabase> ExtractGuids(ToolSettings settings, string path, string name, string message) {
        Profiling.Begin(name, message);
        var db = await GuidMapping.ExtractGuids(settings, path);
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
