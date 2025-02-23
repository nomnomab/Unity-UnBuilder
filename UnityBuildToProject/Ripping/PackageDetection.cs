using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Nomnom;

public sealed class PackageDetection {
    private readonly ExtractData _extractData;
    
    public PackageDetection(ExtractData extractData) {
        _extractData = extractData;
    }
    
    /// <summary>
    /// Extracts the packages from a dummy project that uses project settings from
    /// the AssetRipper exported project.
    /// </summary>s
    public async Task<UnityPackages> GetPackagesFromVersion(UnityPath unityPath, ExtractData extractData) {
        var projectPath     = extractData.Config.ProjectRootPath;
        var tempProjectPath = Path.Combine(projectPath, "..", "TempProject");
        Directory.CreateDirectory(projectPath);
        
        CopyOverScript(tempProjectPath, "RouteStdOutput");
        
        // create a dummy script that will instantly run on load
        // which will extract all of the package information for this unity version
        CopyOverScript(tempProjectPath, "ExtractUnityVersionPackages");
        
        CopyFilesRecursively(
            Path.Combine(projectPath, "ProjectSettings"), 
            Path.Combine(tempProjectPath, "ProjectSettings")
        );
        
        AnsiConsole.MarkupLine("[yellow]Fetching packages from project.[/]");
        
        var stepTree = new Panel(@"
1. Rebuilds the Library folder
2. Imports base packages
3. Opens Unity and fetches the packages".TrimStart()) {
            Header = new PanelHeader("Upcoming steps")
        };
        AnsiConsole.Write(stepTree);
        AnsiConsole.WriteLine("This can take a few minutes!");
        
        await Task.Delay(3000);
        
        await UnityCLI.OpenProjectWithArgs("Fetching packages...", unityPath, tempProjectPath, 
            true,
            "-disable-assembly-updater",
            "-silent-crashes",
            "-batchmode",
            "-logFile -", 
            "-executeMethod Nomnom.ExtractUnityVersionPackages.OnLoad",
            "-exit",
            "| Write-Output"
        );
        
        // now parse the file the extractor created
        var filePath = Path.Combine(tempProjectPath, "packages_output.json");
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException(filePath);
        }
        
        // get the package list
        var packagesOutputJson = File.ReadAllText(filePath);
        var packageOutput      = JsonSerializer.Deserialize<UnityPackages>(packagesOutputJson);
        if (packageOutput == null) {
            throw new FileNotFoundException(filePath);
        }
        
        return packageOutput;
    }
    
    private static void CopyOverScript(string projectPath, string name, Func<string, string>? updateText = null) {
        var folder = GetEditorScriptFolder(projectPath);
        Directory.CreateDirectory(folder);
        
        var scriptPath   = Path.Combine(folder, $"{name}.cs");
        var path         = Path.Combine(Program.OutputFolder, "Resources", $"{name}.cs.txt");
        using var stream = new StreamReader(path);
        var contents     = stream.ReadToEnd();
        
        if (updateText != null) {
            contents = updateText(contents);
        }
        
        File.WriteAllText(scriptPath, contents);
    }
    
    private static string GetEditorScriptFolder(string projectPath) {
        return Path.Combine(projectPath, "Assets", "Editor");
    }
    
    /// <summary>
    /// Attempts to match the project DLLs with known package associations.
    /// </summary>
    public PackageTree TryToMapPackagesToProject(UnityPackages versionPackages) {
        var foundPackages  = new List<PackageInfo>();
        var failedPackages = new List<string>();
        
        foreach (var name in GetGameAssemblies()) {
            var package = PackageAssociations.FindAssociationFromDll(name);
            if (package == null) {
                failedPackages.Add(name);
            } else {
                if (foundPackages.Contains(package)) {
                    continue;
                }
                foundPackages.Add(package);
            }
        }
        
        // print out the output of the associations
        var foundPanel = new Tree("[green]Found Packages[/]");
        foreach (var found in foundPackages.OrderBy(x => x.Id)) {
            foundPanel.AddNode(found.Id);
        }
        AnsiConsole.Write(foundPanel);
        
        var failedPanel = new Tree("[red]DLLs With No Matches[/]");
        foreach (var failed in failedPackages.OrderBy(x => x)) {
            failedPanel.AddNode(failed);
        }
        AnsiConsole.Write(failedPanel);
        
        // build a dependency tree
        var packageTree = PackageTree.Build(foundPackages, versionPackages);
        packageTree.WriteToConsole();
        
        return packageTree;
    }
    
    public async Task ImportPackages(UnityPath unityPath, ExtractData extractData, PackageTree packageTree) {
        var projectPath     = extractData.Config.ProjectRootPath;
        var tempProjectPath = Path.Combine(projectPath, "..", "TempProject");
        // var packagesPath    = Path.Combine(tempProjectPath, "Packages");
        // var manifestPath    = Path.Combine(packagesPath, "manifest.json");
        
        CopyOverScript(tempProjectPath, "RouteStdOutput");
        
        var packageList = packageTree.GetList().ToArray();
        CopyOverScript(tempProjectPath, "InstallPackages", x => {
            return x
                .Replace("#_PACKAGES_TO_INSTALL_", string.Join(",\n", packageList
                    .Select(x => {
                        if (!string.IsNullOrEmpty(x.Item2)) {
                            return $"{x.Item1}@{x.Item2}";
                        }
                        
                        return x.Item1;
                    })
                    .Select(x => $"\"{x}\"")
                ))
                .Replace("#_PACKAGE_COUNT", packageList.Length.ToString());
        });
        
        if (packageTree.Has("com.unity.inputsystem")) {
            // if we have the new input system, add a "enableNativePlatformBackendsForNewInputSystem"
            // field to supress the popup box
            var projectSettingsPath = Path.Combine(tempProjectPath, "ProjectSettings", "ProjectSettings.asset");
            var projectSettingsContent = File.ReadAllLines(projectSettingsPath);
            
            // insert after cloudEnabled
            var sb = new StringBuilder();
            foreach (var line in projectSettingsContent) {
                sb.AppendLine(line);
                if (line.StartsWith("  cloudEnabled:")) {
                    sb.AppendLine("  enableNativePlatformBackendsForNewInputSystem: 1");
                }
            }
            
            File.WriteAllText(projectSettingsPath, sb.ToString());
        }
        
        // parse manifest dependencies
        // var manifestJson    = File.ReadAllText(manifestPath);
        // var manifestData    = JsonSerializer.Deserialize<PackageManfiest>(manifestJson);
        
        // foreach (var (name, version) in packageTree.GetList()) {
        //     if (manifestData!.dependencies.ContainsKey(name)) continue;
        //     manifestData!.dependencies.Add(name, version);
        // }

        // var options = new JsonSerializerOptions(JsonSerializerDefaults.General) {
        //     WriteIndented = true
        // };
        // manifestJson = JsonSerializer.Serialize(manifestData, options);
        // File.WriteAllText(manifestPath, manifestJson);

        AnsiConsole.MarkupLine("[yellow]Installing packages into project.[/]");
        
        var stepTree = new Panel(@"
1. Installs each package in sequence
2. Closes once complete".TrimStart()) {
            Header = new PanelHeader("Upcoming steps")
        };
        AnsiConsole.Write(stepTree);
        AnsiConsole.WriteLine("This can take a while!");
        
        await Task.Delay(3000);
        
        // await UnityCLI.OpenProjectWithArgs("Installing packages...", unityPath, tempProjectPath, "-executeMethod InstallPackages.OnLoad", "-exit");
        
        // todo: handle error routing
        await UnityCLI.OpenProjectWithArgs("Installing packages...", unityPath, tempProjectPath, 
            true,
            "-disable-assembly-updater",
            "-silent-crashes",
            "-batchmode",
            "-logFile -",
            "-executeMethod Nomnom.InstallPackages.OnLoad",
            "-exit",
            "| Write-Output"
        );
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]Deleting[/] the temporary project folder...");
        Directory.Delete(tempProjectPath, true);
    }
    
    public IEnumerable<string> GetGameAssemblies() {
        var extractRootPath    = _extractData.Config.AuxiliaryFilesPath;
        var gameAssembliesPath = Path.Combine(extractRootPath, "GameAssemblies");
        var dlls               = Directory.GetFiles(gameAssembliesPath, "*.dll", SearchOption.TopDirectoryOnly);
        
        foreach (var dll in dlls.OrderBy(x => x)) {
            if (dll == null) continue;
            
            var fileName = Path.GetFileNameWithoutExtension(dll);
            if (PackageAssociations.ExcludePrefixes.Any(x => fileName.StartsWith(x))) {
                continue;
            }
            
            if (PackageAssociations.ExcludeNames.Contains(fileName)) {
                continue;
            }
            
            yield return fileName;
        }
    }
    
    static void CopyFilesRecursively(string sourcePath, string targetPath) {
        // Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        // Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories)) {
            var to = newPath.Replace(sourcePath, targetPath);
            var folder = Path.GetDirectoryName(to);
            Directory.CreateDirectory(folder!);
            File.Copy(newPath, to, true);
        }
    }
}

public class PackageManfiest {
    public Dictionary<string, string> dependencies { get; set; } = [];
}

public class UnityPackages {
    public UnityPackage[]? Packages { get; set; }
    
    public UnityPackage? FindById(string id) {
        return Packages?.FirstOrDefault(x => {
            if (x.m_PackageId == null) {
                return false;
            }
            
            var indedOfAmp = x.m_PackageId.IndexOf('@');
            if (indedOfAmp == -1) {
                return x.m_PackageId == id;
            }
            
            return x.m_PackageId[..indedOfAmp] == id;
        });
    }
}

public class UnityPackage {
    public string? m_PackageId { get; set; }
    public bool? m_IsDirectDependency { get; set; }
    public string? m_Version { get; set; }
    public uint? m_Source { get; set; }
    public string? m_ResolvedPath { get; set; }
    public string? m_AssetPath { get; set; }
    public string? m_Name { get; set; }
    public string? m_DisplayName { get; set; }
    public string? m_Category { get; set; }
    public string? m_Type { get; set; }
    public string? m_Description { get; set; }
    public uint? m_Status { get; set; }
    // public string[]? m_Errors { get; set; }
    public required UnityPackageVersions m_Versions { get; set; }
    public UnityPackageDependency[]? m_Dependencies { get; set; }
    public string[]? m_ResolvedDependencies { get; set; }
    public string[]? m_Keywords { get; set; }
    public UnityPackageAuthor? m_Author { get; set; }
    public bool? m_HasRegistry { get; set; }
    public UnityPackageRegistry? m_Registry { get; set; }
    public bool? m_HideInEditor { get; set; }
    public ulong? m_DatePublishedTicks { get; set; }
}

public class UnityPackageVersions {
    public string[]? m_All { get; set; }
    public string[]? m_Compatible { get; set; }
    public string? m_Recommended { get; set; }
}

public class UnityPackageDependency {
    public string? m_Name { get; set; }
    public string? m_Version { get; set; }
}

public class UnityPackageAuthor {
    public string? m_Name { get; set; }
    public string? m_Email { get; set; }
    public string? m_Url { get; set; }
}

public class UnityPackageRegistry {
    public string? m_Name { get; set; }
    public string? m_Url { get; set; }
    public bool? m_IsDefault { get; set; }
}
