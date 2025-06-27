using Microsoft.CodeAnalysis;

namespace Nomnom;

public record RoslynDatabase {
    public required Dictionary<string, string> FullNameToFilePath { get; set; }
    public required Dictionary<string, List<string>> ShaderNameToFilePaths { get; set; }
    
    public static async Task<RoslynDatabase> Parse(string folderPath) {
        var db = new RoslynDatabase() {
            FullNameToFilePath    = [],
            ShaderNameToFilePaths = [],
        };
        
        var files               = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(x => !Path.GetFileName(x).StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes"))
            .ToArray();
        var scripts             = files
            .Where(x =>  x.EndsWith(".cs"))
            .Where(x => !x.EndsWith(".gen.cs"));
        var shaders             = files.Where(x => x.EndsWith(".shader"));
        var namespacePartsCache = new List<string>(capacity: 128);
        var types               = new List<string>(capacity: 1024);
        
        // todo: split into multiple tasks
        
        // scripts
        foreach (var script in scripts) {
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(script)}\"...");
            
            try {
                await RoslynUtility.ParseTypesFromFile(script, namespacePartsCache, types);
            } catch {
                Console.WriteLine($"Failed to parse \"{Utility.ClampPathFolders(script)}\"");
                types.Clear();
                continue;
            }
            
            foreach (var type in types) {
                if (!db.FullNameToFilePath.TryAdd(type, script)) {
                    var existing = db.FullNameToFilePath[type];
                    Console.WriteLine($" - \"{type}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{script}\"");
                }
            }
            types.Clear();
        }
        
        // shaders
        foreach (var shader in shaders) {
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(shader)}\"...");
            
            try {
                var shaderFile = UnityAssetTypes.ParseShaderFile(shader);
                if (shaderFile == null) {
                    continue;
                }
                
                var name = shaderFile.Name;
                if (!db.FullNameToFilePath.TryAdd(name, shader)) {
                    var existing = db.FullNameToFilePath[name];
                    Console.WriteLine($" - \"{name}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{shader}\"");
                }
                
                if (!db.ShaderNameToFilePaths.TryGetValue(name, out var paths)) {
                    paths = [];
                    db.ShaderNameToFilePaths.Add(name, paths);
                }
                
                paths.Add(shaderFile.FilePath);
            } catch {
                Console.WriteLine($"Failed to parse \"{Utility.ClampPathFolders(shader)}\"");
                continue;
            }
        }
        
        return db;
    }
    
    /// <summary>
    /// Takes the type info from this database and merges any available
    /// guid data into the <paramref name="databases"/>.
    /// </summary>
    /// <param name="databases">The databases being written to, most likely the final project.</param>
    public IEnumerable<RoslynDatabaseMerge> GetMergeUnion(RoslynDatabase[] databases) {
        // find the scripts that are in this database and any of the provided ones
        foreach (var (fullName, filePathFrom) in FullNameToFilePath) {
            // Console.WriteLine($"Checking dbs for {fullName} + {filePathFrom}");
            
            foreach (var db in databases) {
                if (!db.FullNameToFilePath.TryGetValue(fullName, out var filePathTo)) {
                    // Console.WriteLine($" - not in idx db");
                    continue;
                }
                
                if (filePathFrom == filePathTo) {
                    // Console.WriteLine($" - {filePathFrom} == {filePathTo}");
                    continue;
                }

                // Console.WriteLine($" - merge:\nfrom: {filePathFrom}\nto: {filePathTo}");
                yield return new RoslynDatabaseMerge(this, db, filePathFrom, filePathTo);
            }
        }
    }
    
    public static UnityGuid[] MergeInto(GuidDatabase[] guidDatabases, RoslynDatabase[] typeDatabases, GuidDatabaseMerge[] additionalReplacements) {
        var guidDb = guidDatabases[0];
        var typeDb = typeDatabases[0];
        
        var merges    = typeDb.GetMergeUnion(typeDatabases[1..]);
        var toReplace = new HashSet<GuidDatabaseMerge>(capacity: 512);
        var fromGuids = new HashSet<UnityGuid>(capacity: 1024);
        
        var logFile = Path.Combine(Paths.ToolLogsFolder, "merge_into.log");
        File.Delete(logFile);
        
        using (var writer = new StreamWriter(logFile)) {
            // merge types
            foreach (var merge in merges) {
                writer.WriteLine($"checking merge\n{merge}");
                
                if (!guidDb.FilePathToGuid.TryGetValue(merge.FilePathFrom, out var guidFrom)) {
                    writer.WriteLine($"No guid for \"{Utility.ClampPathFolders(merge.FilePathFrom)}\"");
                    continue;
                }
                
                UnityGuid? guidTo = null;
                for (int i = 1; i < guidDatabases.Length; i++) {
                    var db = guidDatabases[i];
                    if (db.FilePathToGuid.TryGetValue(merge.FilePathTo, out guidTo)) {
                        break;
                    }
                }
                
                if (guidTo == null) {
                    writer.WriteLine($"No guidTo for \"{Utility.ClampPathFolders(merge.FilePathTo)}\"");
                    continue;
                }
                
                if (guidFrom == guidTo) {
                    continue;
                }
                
                if (!fromGuids.Add(guidFrom)) {
                    // if it is the same file name, just ignore this
                    // can be from a different root project
                    // todo: handle this better?
                    continue;
                }
                
                // override!
                toReplace.Add(new GuidDatabaseMerge(guidFrom, guidTo, null, null));
                writer.WriteLine($"toReplace.Add:\n - {merge}\n - {new GuidDatabaseMerge(guidFrom, guidTo, null, null)}");
            }
            
            // merge guids
            foreach (var merge in toReplace) {
                // find things that use the original guid
                writer.WriteLine($"{merge.GuidFrom} to {merge.GuidTo}");
                
                if (!guidDb.AssociatedFilePaths.TryGetValue(merge.GuidFrom, out var list)) {
                    writer.WriteLine($" - No associated file path");
                    continue;
                }
                
                foreach (var association in list) {
                    var shortPath = Utility.ClampPathFolders(association);
                    writer.WriteLine($" - [associated] {shortPath}");
                }
            }
            
            // merge additional guids
            foreach (var replace in additionalReplacements) {
                var existing = toReplace.FirstOrDefault(x => x.GuidFrom == replace.GuidTo);
                if (existing != null) {
                    var newReplace = new GuidDatabaseMerge(replace.GuidFrom, existing.GuidTo, null, null);
                    writer.WriteLine($"[exists]\nfrom: {replace}\nto: {newReplace}");
                    toReplace.Add(newReplace);
                } else {
                    toReplace.Add(replace);
                }
            }
        }
        
        Console.WriteLine($"{toReplace.Count} to replace");
        
        var replaced = GuidDatabase.ReplaceGuids(toReplace, guidDatabases);
        return [.. replaced];
    }
    
    public static void RemoveAllNewFiles(string projectPath) {
        var assetsPath = Path.Combine(projectPath, "Assets");
        if (!Directory.Exists(assetsPath)) {
            throw new DirectoryNotFoundException(assetsPath);
        }
        
        var files = Directory.GetFiles(assetsPath, "*.new", SearchOption.AllDirectories);
        Parallel.ForEach(files, File.Delete);
    }
    
    public static (HashSet<string> files, HashSet<string> folders) GetExclusionFiles(ToolSettings settings, UnityGuid[] guids, GuidDatabase guidDb, RoslynDatabase typeDb) {
        var exclusionFiles   = new HashSet<string>(1024);
        var exclusionFolders = new HashSet<string>(1024);
        
        // exclude any included package dll folder
        var exportFolder = settings.ExtractData.Config.ProjectRootPath;
        var scriptsFolder = Path.Combine(exportFolder, "Assets", "Scripts");
        if (Directory.Exists(scriptsFolder)) {
            foreach (var dir in Directory.GetDirectories(scriptsFolder, "*", SearchOption.TopDirectoryOnly)) {
                var name = Path.GetFileNameWithoutExtension(dir);
                if (PackageAssociations.FindAssociationsFromDll(name).Any() || PackageDatabase.ExcludeAssembliesFromProject.Contains(name) || PackageDatabase.IgnoreAssemblyPrefixes.Any(name.StartsWith)) {
                    exclusionFolders.Add(dir);
                }
            }
        }
        
        // exclude any folder that matches a plugins dll
        var projectFolder = settings.ExtractData.GetProjectPath();
        var pluginsFolder = Path.Combine(projectFolder, "Assets", "Plugins");
        if (Directory.Exists(pluginsFolder)) {
            foreach (var dir in Directory.GetFiles(pluginsFolder, "*.dll", SearchOption.TopDirectoryOnly)) {
                var name = Path.GetFileNameWithoutExtension(dir);
                var nameDir = Path.Combine(scriptsFolder, name);
                exclusionFolders.Add(nameDir);
            }
        }
        
        // exclude any mapped guids
        foreach (var guid in guids.Distinct()) {
            Console.WriteLine($"searching for {guid}:");
            
            if (guidDb.Assets.TryGetValue(guid, out var asset)) {
                exclusionFiles.Add(asset.FilePath);
                continue;
            }
            
            if (guidDb.AssociatedFilePaths.TryGetValue(guid, out var paths)) {
                foreach (var path in paths) {
                    exclusionFiles.Add(path);
                }
                continue;
            }
        }
        
        foreach (var path in settings.GameSettings.Files.PathExclusions ?? []) {
            var finalPath = Path.GetFullPath(
                Path.Combine(exportFolder, path)
            );
            
            Console.WriteLine($"path_exclusion:\n - {path}\n - {finalPath}");
            
            if (Directory.Exists(finalPath)) {
                var files = Directory.GetFiles(finalPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files) {
                    exclusionFiles.Add(file);
                }
            }
            
            exclusionFiles.Add(finalPath);
            exclusionFiles.Add(finalPath + ".meta");
        }
        
        // throw null;
        
        return (exclusionFiles, exclusionFolders);
    }
    
    public void WriteToDisk(string name) {
        File.Delete(name);

        using var writer = new StreamWriter(name);
        
        writer.WriteLine("Assets:");
        writer.WriteLine("---------------");
        foreach (var (guid, asset) in FullNameToFilePath) {
            var filePath = Utility.ClampPathFolders(asset);
            writer.WriteLine($"[{guid}] {filePath}");
        }
        
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine("Shaders:");
        writer.WriteLine("---------------");
        foreach (var (shader, paths) in ShaderNameToFilePaths) {
            writer.WriteLine($"[{shader}]");
            foreach (var path in paths) {
                writer.WriteLine($" - {path}");
            }
        }
    }
}

public record RoslynDatabaseMerge(RoslynDatabase DbFrom, RoslynDatabase DbTo, string FilePathFrom, string FilePathTo);
