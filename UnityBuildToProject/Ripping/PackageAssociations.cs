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
            foreach (var name in package.DllNames) {
                if (name.EndsWith('*') && dllName.StartsWith(name[..^1])) {
                    return package;
                }
            }
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
    
    public static readonly string[] ExcludePrefixes = [
        "System",
        // "Mono.",
        "Assembly-CSharp",
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
        "UnityEngine.HotReloadModule"
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
        new() {
            Id = "com.unity.transport",
            DllNames = ["UnityEngine.UNETModule"]
        },
        new() {
            Id = "com.unity.probuilder",
            DllNames = ["Unity.ProBuilder*"]
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
            DllNames = ["UnityEngine.InputModule"]
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
        }
    }
        .OrderBy(x => x.Id)];
}

public record PackageTree {
    public required List<PackageTreeNode> Nodes = [];
    
    public static PackageTree Build(IEnumerable<PackageInfo> packages, UnityPackages versionPackages) {
        var tree = new PackageTree() {
            Nodes = []
        };
        
        var workingPackages = packages.ToList();
        
        // find packages that have no dependencies
        var packagesNoDependencies = workingPackages
            .Select(x => (x, versionPackages.FindById(x.Id)))
            .Where(x => x.Item2 != null && (x.Item2.m_Dependencies == null || x.Item2.m_Dependencies.Length == 0))
            .ToList();
            
        // update the working packages to trim any of the filtered ones above
        workingPackages.RemoveAll(x => packagesNoDependencies.Any(y => y.x == x));
        
        // add the root nodes
        foreach (var package in packagesNoDependencies) {
            tree.Nodes.Add(new PackageTreeNode {
                Info     = package.x,
                Version  = package.Item2?.m_Version ?? "",
                Children = []
            });
        }
        
        // now link up the packages that have a dependency to another.
        foreach (var package in workingPackages) {
            var versionPackage = versionPackages.FindById(package.Id);
            if (versionPackage == null) {
                AnsiConsole.MarkupLine($"[red]Failed[/] to find version package for \"{package.Id}\"");
                continue;
            }
            
            foreach (var dependency in versionPackage.m_Dependencies ?? []) {
                if (dependency.m_Name == null) continue;
                
                var parentNode = tree.Find(dependency.m_Name);
                if (parentNode == null) {
                    // no parent found, make a new one
                    var association = PackageAssociations.FindAssociationFromId(dependency.m_Name);
                    if (association != null) {
                        parentNode = new PackageTreeNode {
                            Info     = association,
                            Version  = dependency.m_Version ?? "",
                            Children = []
                        };
                        
                        tree.Nodes.Add(parentNode);
                    } else {
                        AnsiConsole.MarkupLine($"[red]Failed[/] to find package association for \"{dependency.m_Name}\"");
                        continue;
                    }
                }
                
                parentNode.Children.Add(new PackageTreeNode {
                    Info     = package,
                    Version  = versionPackage.m_Version ?? "",
                    Children = []
                });
            }
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
    
    private PackageTreeNode? Find(string id) {
        foreach (var node in Nodes) {
            var found = _Find(id, node);
            if (found != null) {
                return found;
            }
        }
        
        return null;
        
        // finds a node that matches the id
        static PackageTreeNode? _Find(string id, PackageTreeNode node) {
            if (node.Info.Id == id) {
                return node;
            }
            
            foreach (var child in node.Children) {
                var found = _Find(id, child);
                if (found != null) {
                    return found;
                }
            }
            
            return null;
        }
    }
    
    public void WriteToConsole() {
        var tree = new Tree("Package Tree");
        foreach (var node in Nodes) {
            LogNode(node, tree);
        }
        
        AnsiConsole.Write(tree);
        
        static void LogNode(PackageTreeNode node, Tree tree) {
            var name = $"{node.Info.Id} @ {node.Version}";
            if (node.Children.Count > 0) {
                var nested = new Tree(name);
                tree.AddNode(nested);
                
                foreach (var child in node.Children) {
                    LogNode(child, nested);
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
    public required List<PackageTreeNode> Children = [];
}
