using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// </summary>
    public async Task<UnityPackages> GetPackagesFromVersion(UnityPath unityPath) {
        var projectPath     = _extractData.Config.ProjectRootPath;
        Directory.CreateDirectory(projectPath);
        
        var tempProjectPath = _extractData.GetProjectPath();
        // if (Directory.Exists(tempProjectPath)) {
        //     AnsiConsole.MarkupLine("[red]Deleting[/] previous project...");
            
        //     var files = Directory.GetFiles(tempProjectPath, "*.*", SearchOption.AllDirectories);
        //     foreach (var file in files) {
        //         var shortFile = Utility.ClampPathFolders(file, 4);
        //         AnsiConsole.MarkupLine($"[red]Deleting[/]  \"{shortFile}\"");
        //         File.Delete(file);
        //     }
            
        //     Directory.Delete(tempProjectPath, true);
        // }
        
        Utility.CopyOverScript(tempProjectPath, "RouteStdOutput");
        
        // create a dummy script that will instantly run on load
        // which will extract all of the package information for this unity version
        Utility.CopyOverScript(tempProjectPath, "ExtractUnityVersionPackages");
        
        await Utility.CopyFilesRecursivelyPretty(
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
    
    /// <summary>
    /// Attempts to match the project DLLs with known package associations.
    /// </summary>
    public PackageTree TryToMapPackagesToProject(UnityPackages versionPackages) {
        var foundPackages  = new List<PackageInfo>();
        var failedPackages = new List<string>();
        
        // fetch from game assemblies
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
        
        // for now make sure there is always newtonsoft json included
        if (packageTree.Find("com.unity.nuget.newtonsoft-json") == null) {
            var package = PackageAssociations.FindAssociationFromId("com.unity.nuget.newtonsoft-json");
            packageTree.Nodes.Add(new PackageTreeNode() {
                Info = package!,
                Version = "",
                Children = []
            });
        }
        
        return packageTree;
    }
    
    public void ApplyGameSettingsPackages(GameSettings gameSettings, PackageTree packageTree) {
        if (gameSettings.PackageOverrides == null) {
            Console.WriteLine($"No package overrides");
            return;
        }
        
        var overrides = gameSettings.PackageOverrides;
        if (overrides.Packages == null) {
            Console.WriteLine($"No packages");
            return;
        }
        
        Console.WriteLine($"{overrides.Packages.Length} packages to override");
        
        foreach (var (id, version) in overrides.Packages) {
            // if the package exists, replace the version
            var node = packageTree.Find(id);
            if (node != null && !string.IsNullOrEmpty(version)) {
                if (version == "no") {
                    packageTree.Remove(id);
                    continue;
                }
                
                node.Version = version;
                AnsiConsole.MarkupLine($"[green]Override[/] for \"{id}\" with version \"{version}\"");
                continue;
            }
            
            // if it doesn't, add it
            if (node == null) {
                AnsiConsole.MarkupLine($"[green]Override[/] added \"{id}\" with version \"{version}\"");
                packageTree.Nodes.Add(new PackageTreeNode() {
                    Info = new() {
                        Id = id,
                        ForVersions = [],
                        DllNames = [],
                    },
                    Version = version ?? "",
                    Children = []
                });
            }
        }
    }
    
    public async Task ImportPackages(UnityPath unityPath, PackageTree packageTree) {
        var tempProjectPath = _extractData.GetProjectPath();
        Utility.CopyOverScript(tempProjectPath, "RouteStdOutput");
        
        var packageList = packageTree.GetList().ToArray();
        var packageNames = packageList
            .Select(x => {
                if (!string.IsNullOrEmpty(x.Item2)) {
                    return $"{x.Item1}@{x.Item2}";
                }
                
                return x.Item1;
            });
        Utility.CopyOverScript(tempProjectPath, "InstallPackages", x => {
            return x
                // to install
                .Replace("#_PACKAGES_TO_INSTALL_", string.Join(",\n", 
                    packageNames.Select(x => $"\"{x}\"")
                ))
                .Replace("#_PACKAGE_INSTALL_COUNT", packageList.Length.ToString())
                // to remove
                .Replace("#_PACKAGES_TO_REMOVE_", string.Join(",\n", 
                    PackageAssociations.ExcludeIds.Select(x => $"\"{x}\"")
                ))
                .Replace("#_PACKAGE_REMOVE_COUNT", PackageAssociations.ExcludeIds.Length.ToString());
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
        
        AnsiConsole.MarkupLine("[yellow]Installing packages into project.[/]");
        
        var stepTree = new Panel(@"
1. Writes the packages with versions directory to the manifest
2. Installs each package in sequence
3. Closes once complete".TrimStart()) {
            Header = new PanelHeader("Upcoming steps")
        };
        AnsiConsole.Write(stepTree);
        AnsiConsole.WriteLine("This can take a while!");
        
        await Task.Delay(3000);
        
        // write to the manifest first
        AddPackagesToManifest(tempProjectPath, packageList);
        
        // then install the packages after
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
    }
    
    private static void AddPackagesToManifest(string projectPath, IEnumerable<(string, string)> packages) {
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
        
        // load up the json into a document
        var manifestContents = File.ReadAllText(manifestPath);
        var rootNode         = JsonNode.Parse(manifestContents);
        if (rootNode == null) {
            throw new Exception("Failed to parse manifest file");
        }
        
        var dependencies     = rootNode["dependencies"]?.AsObject() ?? [];
        
        foreach (var package in packages) {
            if (!string.IsNullOrEmpty(package.Item2)) {
                if (package.Item2 == "no") {
                    // AnsiConsole.WriteLine($"Removed {package.Item1} from the manifest");
                    // dependencies.Remove(package.Item1);
                } else {
                    AnsiConsole.WriteLine($"Wrote {package.Item1}@{package.Item2} to the manifest");
                    dependencies[package.Item1] = package.Item2;
                }
            }
        }
        
        var newJson = rootNode.ToJsonString(new JsonSerializerOptions {
            WriteIndented = true
        });
        
        File.WriteAllText(manifestPath, newJson);
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
    
    public void WriteToDisk(string name) {
        var namePath = Path.Combine(Program.LogsFolder, name);
        File.Delete(namePath);
        
        using var writer = new StreamWriter(namePath);
        foreach (var package in Packages ?? []) {
            writer.WriteLine($"{package.m_PackageId}");
            writer.WriteLine(package);
            
            foreach (var dep in package.m_Dependencies ?? []) {
                writer.WriteLine($" - {dep.m_Name} @ {dep.m_Version}");
            }
        }
    }
}

public record UnityPackage {
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

public record UnityPackageVersions {
    public string[]? m_All { get; set; }
    public string[]? m_Compatible { get; set; }
    public string? m_Recommended { get; set; }
}

public record UnityPackageDependency {
    public string? m_Name { get; set; }
    public string? m_Version { get; set; }
}

public record UnityPackageAuthor {
    public string? m_Name { get; set; }
    public string? m_Email { get; set; }
    public string? m_Url { get; set; }
}

public record UnityPackageRegistry {
    public string? m_Name { get; set; }
    public string? m_Url { get; set; }
    public bool? m_IsDefault { get; set; }
}
