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
    public async Task<UnityPackages> GetPackagesFromVersion(ToolSettings settings, string newProjectPath) {
        var projectPath    = _extractData.Config.ProjectRootPath;
        
        Utility.CopyOverScript(newProjectPath, "RouteStdOutput");
        
        if (!settings.ProgramArgs.SkipPackageFetching) {
            // create a dummy script that will instantly run on load
            // which will extract all of the package information for this unity version
            Utility.CopyOverScript(newProjectPath, "ExtractUnityVersionPackages");
            
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
            
            var unityPath = settings.GetUnityPath();
            await UnityCLI.OpenProjectHiddenNoQuit("Fetching packages...", unityPath, true, newProjectPath,
                "-executeMethod Nomnom.ExtractUnityVersionPackages.OnLoad"
            );
        }
        
        // now parse the file the extractor created
        var filePath = Path.Combine(newProjectPath, "..", "packages_output.json");
        if (!File.Exists(filePath)) {
            if (settings.ProgramArgs.SkipPackageFetching) {
                AnsiConsole.MarkupLine("[red]Error[/]: No packages_output.json found, make sure you run without --skip_pack at least one time.");
            }
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
    
    public async Task ImportPackages(ToolSettings settings, PackageTree packageTree) {
        var tempProjectPath = _extractData.GetProjectPath();
        Utility.CopyOverScript(tempProjectPath, "RouteStdOutput");
        
        if (settings.ProgramArgs.SkipPackageAll) return;
        
        var packageList = packageTree.GetList().ToArray();
        var packageNames = packageList
            .Select(x => {
                if (!string.IsNullOrEmpty(x.Item2)) {
                    return $"{x.Item1}@{x.Item2}";
                }
                
                return x.Item1;
            });
        
        AnsiConsole.WriteLine($"package names: {string.Join('\n', packageNames)}");
        
        Utility.CopyOverScript(tempProjectPath, "InstallPackages", x => {
            var newText = x
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
            
            AnsiConsole.WriteLine(newText);
            return newText;
        });
        
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
        AddPackagesToManifest(settings.ProgramArgs, tempProjectPath, packageList);
        
        // then install the packages after
        var unityPath = settings.GetUnityPath();
        await UnityCLI.OpenProjectHidden("Installing packages...", unityPath, true, tempProjectPath,
            "-executeMethod Nomnom.InstallPackages.OnLoad"
        );
        
        if (packageTree.Has("com.unity.inputsystem")) {
            // if we have the new input system, add a "enableNativePlatformBackendsForNewInputSystem"
            // field to supress the popup box
            // can be different depending on unity version
            var projectSettingsPath = Path.Combine(tempProjectPath, "ProjectSettings", "ProjectSettings.asset");
            var projectSettingsContent = File.ReadAllLines(projectSettingsPath);
            
            // insert after cloudEnabled
            var sb = new StringBuilder();
            foreach (var line in projectSettingsContent) {
                sb.AppendLine(line);
                
                // 2019
                // also has: disableOldInputManagerSupport
                if (line.StartsWith("  cloudEnabled:")) {
                    Console.WriteLine($"Replaced enableNativePlatformBackendsForNewInputSystem");
                    sb.AppendLine("  enableNativePlatformBackendsForNewInputSystem: 1");
                    continue;
                }
                
                // 2020, 2021
                if (line.StartsWith("  activeInputHandler:")) {
                    Console.WriteLine($"Replaced activeInputHandler");
                    sb.AppendLine("  activeInputHandler: 2");
                    continue;
                }
            }
            
            File.WriteAllText(projectSettingsPath, sb.ToString());
        }
        
        // cache the manifest
        // var manifestPath = Path.Combine(tempProjectPath, "Packages", "manifest.json");
        // var cachePath    = Path.Combine(tempProjectPath, "..", "manifest.json");
        // File.Copy(manifestPath, cachePath, true);
        
        // var lockFilePath = Path.Combine(tempProjectPath, "Packages", "packages-lock.json");
        // if (File.Exists(lockFilePath)) {
        //     cachePath        = Path.Combine(tempProjectPath, "..", "packages-lock.json");
        //     File.Copy(lockFilePath, cachePath, true);
        // }
    }
    
    private static void AddPackagesToManifest(ProgramArgs args, string projectPath, IEnumerable<(string, string)> packages) {
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
        // var cachePath    = Path.Combine(projectPath, "..", "manifest.json");
        
        if (args.SkipPackageFetching) {
            // if (!File.Exists(cachePath)) {
            //     AnsiConsole.MarkupLine($"[red]Error[/]: no cache manifest file found, make sure you run without --skip_pack at least once.");
            //     throw new FileNotFoundException(cachePath);
            // }
            
            // File.Copy(cachePath, manifestPath, true);
            return;
        }
        
        if (!File.Exists(manifestPath)) {
            AnsiConsole.MarkupLine($"[red]Error[/]: no manifest file found");
            throw new FileNotFoundException(manifestPath);
        }
        
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
        
        foreach (var package in PackageAssociations.ExcludeIds) {
            try {
                dependencies.Remove(package);
            } catch { }
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
            if (PackageAssociations.ExcludePrefixesFromPackages.Any(x => fileName.StartsWith(x))) {
                continue;
            }
            
            if (PackageAssociations.ExcludeNamesFromPackages.Contains(fileName)) {
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
        var namePath = Path.Combine(Paths.LogsFolder, name);
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
