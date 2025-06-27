using Spectre.Console;

namespace Nomnom;

public record PackageInfo {
    public required string Id;
    public required string[] DllNames;
    public string[]? ForVersions;
}

public static class PackageAssociations {
    public static IEnumerable<PackageInfo> FindAssociationsFromDll(string dllName) {
        if (dllName.EndsWith(".dll")) {
            dllName = dllName[..^".dll".Length];
        }
        
        foreach (var package in PackageDatabase.Packages) {
            if (package.DllNames.Contains(dllName)) {
                yield return package;
            }
        }
        
        // if (PackageDatabase.DllToPackage.TryGetValue(dllName, out var packageName)) {
        //     return PackageDatabase.Packages[packageName];
        // }
    }
    
    public static PackageInfo? FindAssociationFromId(string id) {
        return PackageDatabase.Packages.FirstOrDefault(x => x.Id == id);
        
        // if (PackageDatabase.Packages.TryGetValue(id, out var package)) {
        //     return package;
        // }
        
        // return null;
        
        // return Packages?.FirstOrDefault(x => {
        //     if (x.Id == null) {
        //         return false;
        //     }
            
        //     var indedOfAmp = x.Id.IndexOf('@');
        //     if (indedOfAmp == -1) {
        //         return x.Id == id;
        //     }
            
        //     return x.Id[..indedOfAmp] == id;
        // });
    }
    
    // public static readonly string[] ExcludePrefixDelete = [
    //     "Unity.InternalAPIEngineBridge",
    //     "Unity.ResourceManager",
    // ];
    
    // public static readonly string[] ExcludePrefixesFromPackages = [
    //     "System",
    //     "Assembly-CSharp",
    //     "Unity.InternalAPIEngineBridge",
    //     "FacePunch",
    //     "Unity.Services.",
    // ];
    
    // public static readonly string[] ExcludePrefixesFromProject = [
    //     "System",
    //     "Unity.InternalAPIEngineBridge",
    //     "FacePunch",
    //     "Mono.",
    //     "UnityEngine.",
    //     "Unity.",
    //     "Newtonsoft.Json",
    //     "FishNet."
    // ];
    
    // public static readonly string[] ExcludeNamesFromPackages = [
    //     "mscorlib",
    //     "netstandard",
    //     "UnityEngine",
    //     "UnityEngine.AccessibilityModule",
    //     "UnityEngine.AndroidJNIModule",
    //     "UnityEngine.CoreModule",
    //     "UnityEngine.PerformanceReportingModule",
    //     "UnityEngine.InputLegacyModule",
    //     "UnityEngine.ProfilerModule",
    //     "UnityEngine.UnityAnalyticsModule",
    //     "UnityEngine.UnityConnectModule",
    //     "UnityEngine.UnityTestProtocolModule",
    //     "UnityEngine.SharedInternalsModule",
    //     "UnityEngine.HotReloadModule",
    //     "Unity.ResourceManager",
    //     "Unity.Rider.Editor",
    //     "Boo.Lang",
    //     "UnityScript.Lang",
    //     "dfScriptLite",
    //     "UnityEngine.NVIDIAModule"
    // ];
    
    // public static readonly string[] ExcludeNamesFromProject = [
    //     .. ExcludeNamesFromPackages,
    //     "ClientNetworkTransform",
    //     "Pathfinding.Ionic.Zip.Reduced",
    //     "AstarPathfindingProject"
    // ];
    
    // public static readonly string[] ExcludeIds = [
    //     // "com.unity.test-framework.performance",
    //     // "com.unity.test-framework",
    //     "com.unity.collab-proxy",
    //     "com.unity.ide.rider",
    //     "com.unity.ide.visualstudio",
    //     "com.unity.ide.vscode"
    // ];
}

public record PackageTree {
    public required List<PackageTreeNode> Nodes = [];
    public required List<string> Dlls = [];
    
    public static PackageTree FromFile(string manifest) {
        var tree = new PackageTree() {
            Nodes = [],
            Dlls  = [],
        };
        
        var text = File.ReadAllText(manifest);
        var data = System.Text.Json.JsonSerializer.Deserialize<PackageManifest>(text);
        
        if (data != null) {
            foreach (var (key, value) in data.dependencies) {
                if (string.IsNullOrEmpty(key)) continue;
                if (string.IsNullOrEmpty(value)) continue;
                
                tree.Nodes.Add(new PackageTreeNode() {
                    Info = new PackageInfo() {
                        Id = key,
                        DllNames    = [],
                        ForVersions = [],
                    },
                    Children = [],
                    Version  = value
                });
            }
        }
        
        return tree;
    }
    
    public static PackageTree Build(IEnumerable<PackageInfo> packages, ToolSettings settings, UnityPackages versionPackages) {
        var tree = new PackageTree() {
            Nodes = [],
            Dlls  = [.. PackageDetection.GetGameAssemblies(settings)],
        };
        
        var workingPackages = packages.Where(x => !PackageDatabase.IgnorePackages.Contains(x.Id))
            .ToList();
        
        // find packages that have no dependencies
        var packagesNoDependencies = workingPackages
            .Select(x => (x, versionPackages.FindById(x.Id)))
            .Where(x => x.Item2 != null && (x.Item2.m_Dependencies == null || x.Item2.m_Dependencies.Length == 0))
            .ToList();
            
        // update the working packages to trim any of the filtered ones above
        workingPackages.RemoveAll(x => packagesNoDependencies.Any(y => y.x == x));
        
        // add the root nodes
        foreach (var package in packagesNoDependencies) {
            AnsiConsole.WriteLine($"{package.x.Id} @ {package.Item2?.m_Version ?? ""} has no dependencies");
            
            tree.Nodes.Add(new PackageTreeNode {
                Info     = package.x,
                Version  = package.Item2?.m_Version ?? "",
                Children = []
            });
        }
        
        // now link up the packages that have a dependency to another.
        foreach (var package in workingPackages) {
            AnsiConsole.WriteLine($"Checking package {package.Id}");
            
            var versionPackage = versionPackages.FindById(package.Id);
            if (versionPackage == null) {
                AnsiConsole.MarkupLine($"[red]Failed[/] to find version package for \"{package.Id}\"");
                continue;
            }
            
            // go through each dependency of this package and add it if it is missing
            var dependencies = versionPackage.m_Dependencies ?? [];
            var foundDependency = false;
            foreach (var dependency in dependencies) {
                if (dependency.m_Name == null) continue;
                
                Console.WriteLine($" - checking dependency {dependency.m_Name}");
                
                // find the dependency
                var parentNode = tree.Find(dependency.m_Name);
                if (parentNode == null) {
                    Console.WriteLine($" - no parent found");
                    
                    // no dependency found, make a new one
                    var association = PackageAssociations.FindAssociationFromId(dependency.m_Name);
                    if (association != null) {
                        parentNode = new PackageTreeNode {
                            Info     = association,
                            Version  = dependency.m_Version ?? "",
                            Children = []
                        };
                        
                        tree.Nodes.Add(parentNode);
                        Console.WriteLine($" - new parent node");
                    } else {
                        AnsiConsole.MarkupLine($" - [red]failed[/] to find package association for \"{dependency.m_Name}\"");
                        continue;
                    }
                }
                
                parentNode.Children.Add(package.Id);
                Console.WriteLine($" - added {package.Id} to {parentNode.Info.Id}");
            }
            
            if (tree.Find(package.Id) == null) {
                tree.Nodes.Add(new PackageTreeNode {
                    Info     = package,
                    Version  = versionPackage.m_Version ?? "",
                    Children = []
                });
                
                Console.WriteLine($" - added {package.Id} to tree");
            }
            
            AnsiConsole.WriteLine($"Done checking {package.Id}");
        }
        
        // see if any are left over
        foreach (var working in workingPackages) {
            var node = tree.Find(working.Id);
            if (node == null) {
                AnsiConsole.MarkupLine($"[red]Failed[/] to find owner for \"{working.Id}\"");
            }
        }
        
        return tree;
    }
    
    public bool Has(string id) {
        return Find(id) != null;
    }
    
    public PackageTreeNode? Find(string id) {
        foreach (var node in Nodes) {
            if (node.Info.Id == id) {
                return node;
            }
        }
        
        return null;
    }
    
    public void Remove(string id) {
        for (int i = 0; i < Nodes.Count; i++) {
            var node = Nodes[i];
            if (node.Info.Id == id) {
                Nodes.RemoveAt(i);
            }
        }
    }
    
    public IEnumerable<(string, string)> GetList() {
        return Nodes.SelectMany(GetList);
    }
    
    private IEnumerable<(string, string)> GetList(PackageTreeNode node) {
        yield return (node.Info.Id, node.Version);
            
        foreach (var child in node.Children) {
            var childNode = Find(child);
            if (childNode != null) {
                yield return (childNode.Info.Id, childNode.Version);
            }
        }
    }
    
    public IEnumerable<PackageTreeNode> GetNodes() {
        return GetAllNodes().DistinctBy(x => x.Info.Id);
    }
    
    private IEnumerable<PackageTreeNode> GetAllNodes() {
        foreach (var node in Nodes) {
            foreach (var n in getChildrenNodes(node)) {
                yield return n;
            }
        }
        
        IEnumerable<PackageTreeNode> getChildrenNodes(PackageTreeNode node) {
            yield return node;
            
            foreach (var child in node.Children) {
                var childNode = Find(child);
                if (childNode != null) {
                    yield return childNode;
                    
                    foreach (var n in getChildrenNodes(childNode)) {
                        yield return n;
                    }
                }
                
                // yield return child;
                
                // foreach (var n in getChildrenNodes(child)) {
                //     yield return n;
                // }
            }
        }
    }
    
    public void WriteToDisk(string name) {
        var namePath = Path.Combine(Paths.ToolLogsFolder, name);
        File.Delete(namePath);
        
        using var writer = new StreamWriter(namePath);
        writer.WriteLine("Nodes");
        writer.WriteLine("-------------");
        
        foreach (var node in Nodes) {
            LogNode(node, 0);
        }
        
        void LogNode(PackageTreeNode node, int ident) {
            var name   = $"{node.Info.Id} @ {node.Version}";
            var prefix = new string(' ', ident * 2);
            writer.WriteLine($"{prefix}{name}");
            
            if (node.Children.Count > 0) {
                foreach (var child in node.Children) {
                    var childNode = Find(child);
                    if (childNode != null) {
                        LogNode(childNode, ident + 1);
                    }
                    
                    // LogNode(child, ident + 1);
                }
            }
        }
    }
    
    public void WriteToConsole() {
        AnsiConsole.WriteLine();
        
        var tree = new Tree("Package Tree");
        foreach (var node in Nodes) {
            LogNode(node, tree);
        }
        
        AnsiConsole.Write(tree);
        
        void LogNode(PackageTreeNode node, Tree tree) {
            var name = $"{node.Info.Id} @ {node.Version}";
            if (node.Children.Count > 0) {
                var nested = new Tree(name);
                tree.AddNode(nested);
                
                foreach (var child in node.Children) {
                    var childNode = Find(child);
                    if (childNode != null) {
                        LogNode(childNode, nested);
                    }
                    
                    // LogNode(child, nested);
                }
            } else {
                tree.AddNode(name);
            }
        }
    }
}

public record PackageTreeNode {
    public required string Version;
    public required PackageInfo Info;
    public required List<string> Children = [];
}
