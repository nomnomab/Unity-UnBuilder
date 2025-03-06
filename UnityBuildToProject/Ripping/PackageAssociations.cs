using Spectre.Console;

namespace Nomnom;

public record PackageInfo {
    public required string Id;
    public required string[] DllNames;
    public string[]? ForVersions;
}

public static class PackageAssociations {
    public static PackageInfo? FindAssociationFromDll(string dllName) {
        foreach (var package in Packages) {
            if (package.DllNames.Contains(dllName)) {
                return package;
            }
            
            // if we have any wildcard endings, handle them here
            // foreach (var name in package.DllNames) {
            //     if (name.EndsWith('*') && dllName.StartsWith(name[..^1])) {
            //         return package;
            //     }
            // }
        }
        
        return null;
    }
    
    public static PackageInfo? FindAssociationFromId(string id) {
        return Packages?.FirstOrDefault(x => {
            if (x.Id == null) {
                return false;
            }
            
            var indedOfAmp = x.Id.IndexOf('@');
            if (indedOfAmp == -1) {
                return x.Id == id;
            }
            
            return x.Id[..indedOfAmp] == id;
        });
    }
    
    public static readonly string[] ExcludePrefixDelete = [
        "Unity.InternalAPIEngineBridge",
        "Unity.ResourceManager",
    ];
    
    public static readonly string[] ExcludePrefixes = [
        "System",
        // "Mono.",
        "Assembly-CSharp",
        "Unity.InternalAPIEngineBridge"
    ];
    
    public static readonly string[] ExcludeNames = [
        "mscorlib",
        "netstandard",
        "UnityEngine",
        "UnityEngine.AccessibilityModule",
        "UnityEngine.AndroidJNIModule",
        "UnityEngine.CoreModule",
        "UnityEngine.PerformanceReportingModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.ProfilerModule",
        "UnityEngine.UnityAnalyticsModule",
        "UnityEngine.UnityConnectModule",
        "UnityEngine.UnityTestProtocolModule",
        "UnityEngine.SharedInternalsModule",
        "UnityEngine.HotReloadModule",
        "Unity.ResourceManager",
        "Unity.Rider.Editor",
        "Boo.Lang",
        "UnityScript.Lang",
        "dfScriptLite"
    ];
    
    public static readonly string[] ExcludeIds = [
        // "com.unity.test-framework.performance",
        // "com.unity.test-framework",
        "com.unity.collab-proxy",
        "com.unity.ide.rider",
        "com.unity.ide.visualstudio",
        "com.unity.ide.vscode"
    ];
    
    public static readonly PackageInfo[] Packages = [.. new PackageInfo[] {
        new() {
            Id = "com.unity.modules.ui",
            DllNames = [
                "UnityEngine.UI",
                "UnityEngine.UIModule", 
                "UnityEngine.TextCoreModule", 
                "UnityEngine.TextRenderingModule"
            ]
        },
        new() {
            Id = "com.unity.ugui",
            DllNames = [
                "UnityEngine.UI",
                "UnityEngine.UIModule", 
                "UnityEngine.TextCoreModule", 
                "UnityEngine.TextRenderingModule"
            ]
        },
        new() {
            Id = "com.unity.modules.uielements",
            DllNames = ["UnityEngine.UIElementsModule"]
        },
        // new() {
        //     Id = "com.unity.transport",
        //     DllNames = ["UnityEngine.UNETModule"]
        // },
        new() {
            Id = "com.unity.probuilder",
            DllNames = [
                "Unity.ProBuilder",
                "Unity.ProBuilder.KdTree",
                "Unity.ProBuilder.Poly2Tri",
                "Unity.ProBuilder.Stl",
                "Unity.ProBuilder.Csg"
            ]
        },
        new() {
            Id = "com.unity.textmeshpro",
            DllNames = ["Unity.TextMeshPro"]
        },
        new() {
            Id = "com.unity.timeline",
            DllNames = ["Unity.Timeline"]
        },
        new() {
            Id = "com.unity.vr",
            DllNames = ["UnityEngine.VRModule"]
        },
        new() {
            Id = "com.unity.xr",
            DllNames = ["UnityEngine.XRModule"]
        },
        new() {
            Id = "com.unity.wind",
            DllNames = ["UnityEngine.WindModule"]
        },
        new() {
            Id = "com.unity.modules.terrain",
            DllNames = ["UnityEngine.TerrainModule"]
        },
        new() {
            Id = "com.unity.modules.terrainphysics",
            DllNames = ["UnityEngine.TerrainPhysicsModule"]
        },
        new() {
            Id = "com.unity.modules.physics",
            DllNames = ["UnityEngine.PhysicsModule"]
        },
        new() {
            Id = "com.unity.modules.physics2d",
            DllNames = ["UnityEngine.Physics2DModule"]
        },
        new() {
            Id = "com.unity.modules.imageconversion",
            DllNames = ["UnityEngine.ImageConversionModule"]
        },
        new() {
            Id = "com.unity.modules.unitywebrequest",
            DllNames = ["UnityEngine.UnityWebRequestModule"]
        },
        new() {
            Id = "com.unity.modules.unitywebrequestwww",
            DllNames = ["UnityEngine.UnityWebRequestWWWModule"]
        },
        new() {
            Id = "com.unity.modules.unitywebrequesttexture",
            DllNames = ["UnityEngine.UnityWebRequestTextureModule"]
        },
        new() {
            Id = "com.unity.modules.unitywebrequestassetbundle",
            DllNames = ["UnityEngine.UnityWebRequestAssetBundleModule"]
        },
        new() {
            Id = "com.unity.modules.unitywebrequestaudio",
            DllNames = ["UnityEngine.UnityWebRequestAudioModule"]
        },
        new() {
            Id = "com.unity.modules.umbra",
            DllNames = ["UnityEngine.UmbraModule"]
        },
        new() {
            Id = "com.unity.modules.screencapture",
            DllNames = ["UnityEngine.ScreenCaptureModule"]
        },
        new() {
            Id = "com.unity.inputsystem",
            DllNames = [
                "Unity.InputSystem",
                "Unity.InputSystem.ForUI"
            ]
        },
        new() {
            Id = "com.unity.progrids",
            DllNames = ["Unity.ProGrids"]
        },
        new() {
            Id = "com.unity.modules.cloth",
            DllNames = ["UnityEngine.ClothModule"]
        },
        new() {
            Id = "com.unity.modules.ai",
            DllNames = ["UnityEngine.AIModule"]
        },
        new() {
            Id = "com.unity.modules.animation",
            DllNames = ["UnityEngine.AnimationModule"]
        },
        new() {
            Id = "com.unity.modules.video",
            DllNames = ["UnityEngine.VideoModule"]
        },
        new() {
            Id = "com.unity.modules.audio",
            DllNames = ["UnityEngine.AudioModule"]
        },
        new() {
            Id = "com.unity.xr.arcore",
            DllNames = ["UnityEngine.ARModule"]
        },
        new() {
            Id = "com.unity.postprocessing",
            DllNames = ["Unity.Postprocessing.Runtime"]
        },
        new() {
            Id = "com.unity.modules.assetbundle",
            DllNames = ["UnityEngine.AssetBundleModule"]
        },
        new() {
            Id = "com.unity.modules.director",
            DllNames = ["UnityEngine.DirectorModule"]
        },
        new() {
            Id = "com.unity.modules.particlesystem",
            DllNames = ["UnityEngine.ParticleSystemModule"]
        },
        new() {
            Id = "com.unity.2d.sprite",
            DllNames = ["UnityEngine.SpriteMaskModule"]
        },
        new() {
            Id = "com.unity.2d.spriteshape",
            DllNames = ["UnityEngine.SpriteShapeModule"]
        },
        new() {
            Id = "com.unity.2d.tilemap",
            DllNames = ["UnityEngine.TilemapModule"]
        },
        new() {
            Id = "com.unity.modules.vehicles",
            DllNames = ["UnityEngine.VehiclesModule"]
        },
        new() {
            Id = "com.unity.visualeffectgraph",
            DllNames = ["UnityEngine.VFXModule"]
        },
        new() {
            Id = "com.unity.modules.jsonserialize",
            DllNames = ["UnityEngine.JSONSerializeModule"]
        },
        new() {
            Id = "com.unity.modules.imgui",
            DllNames = ["UnityEngine.IMGUIModule"]
        },
        new() {
            Id = "com.unity.multiplayer-hlapi",
            DllNames = [
                "com.unity.multiplayer-hlapi.Editor",
                "com.unity.multiplayer-hlapi.Runtime",
                "com.unity.multiplayer-weaver.Editor"
            ]
        },
        new() {
            Id = "com.unity.postprocessing",
            DllNames = ["Unity.Postprocessing.Runtime"]
        },
        new() {
            Id = "com.unity.render-pipelines.core",
            DllNames = [
                "Unity.RenderPipelines.Core.Runtime",
                "Unity.RenderPipelines.Core.ShaderLibrary",
            ]
        },
        new() {
            Id = "com.unity.shadergraph",
            DllNames = [
                "Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary"
                ]
        },
        new() {
            Id = "com.unity.render-pipelines.universal",
            DllNames = [
                "Unity.RenderPipelines.Universal.Config.Runtime",
                "Unity.RenderPipelines.Universal.Runtime",
                "Unity.RenderPipelines.Universal.Shaders",
                "Unity.RenderPipeline.Universal.ShaderLibrary"
            ]
        },
        new() {
            Id = "com.unity.burst",
            DllNames = [
                "Unity.Burst",
                "Unity.Burst.Unsafe"
            ]
        },
        new() {
            Id = "com.unity.mathematics",
            DllNames = ["Unity.Mathematics"]
        },
        new() {
            Id = "com.unity.cinemachine",
            DllNames = ["Cinemachine"]
        },
        new() {
            Id = "com.unity.visualscripting",
            DllNames = [
                "Unity.VisualScripting.Core",
                "Unity.VisualScripting.Flow",
                "Unity.VisualScripting.State",
                "Unity.VisualScripting.Antlr3.Runtime"
            ]
        },
        new() {
            Id = "com.unity.recorder",
            DllNames = [
                "Unity.Recorder",
                "Unity.Recorder.Base"
            ]
        },
        new() {
            Id = "com.unity.localization",
            DllNames = ["Unity.Localization"]
        },
        new() {
            Id = "com.unity.nuget.newtonsoft-json",
            DllNames = ["Newtonsoft.Json"]
        },
        new() {
            Id = "com.unity.addressables",
            DllNames = ["Unity.Addressables"]
        },
        new() {
            Id = "com.autodesk.fbx",
            DllNames = ["Autodesk.Fbx"]
        },
        new() {
            Id = "com.unity.formats.fbx",
            DllNames = ["Unity.Formats.Fbx.Runtime"]
        },
        new() {
            Id = "com.unity.ai.navigation",
            DllNames = ["Unity.AI.Navigation"]
        },
        new() {
            Id = "com.unity.scriptablebuildpipeline",
            DllNames = ["Unity.ScriptableBuildPipeline"]
        },
    }
        .OrderBy(x => x.Id)];
}

public record PackageTree {
    public required List<PackageTreeNode> Nodes = [];
    
    public static PackageTree Build(IEnumerable<PackageInfo> packages, UnityPackages versionPackages) {
        var tree = new PackageTree() {
            Nodes    = [],
        };
        
        var workingPackages = packages.Where(x => !PackageAssociations.ExcludeIds.Contains(x.Id))
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
        
        // finds a node that matches the id
        // static PackageTreeNode? _Find(string id, PackageTreeNode node) {
        //     if (node.Info.Id == id) {
        //         return node;
        //     }
            
        //     foreach (var child in node.Children) {
        //         var found = _Find(id, child);
        //         if (found != null) {
        //             return found;
        //         }
        //     }
            
        //     return null;
        // }
    }
    
    public void Remove(string id) {
        for (int i = 0; i < Nodes.Count; i++) {
            var node = Nodes[i];
            if (node.Info.Id == id) {
                Nodes.RemoveAt(i);
                return;
            }
            
            // _Find(id, node);
        }
        
        // finds a node that matches the id
        // static void _Find(string id, PackageTreeNode node) {
        //     for (int i = 0; i < node.Children.Count; i++) {
        //         var child = node.Children[i];
        //         if (child.Info.Id == id) {
        //             node.Children.RemoveAt(i);
        //             return;
        //         }
                
        //         _Find(id, child);
        //     }
        // }
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
            
            // foreach (var c in GetList(child)) {
            //     yield return c;
            // }
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
        var namePath = Path.Combine(Paths.LogsFolder, name);
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
