
using System.Text.RegularExpressions;

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
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(script, 6)}\"...");
            
            try {
                await RoslynUtility.ParseTypesFromFile(script, namespacePartsCache, types);
            } catch {
                Console.WriteLine($"Failed to parse \"{Utility.ClampPathFolders(script, 6)}\"");
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
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(shader, 6)}\"...");
            
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
                Console.WriteLine($"Failed to parse \"{Utility.ClampPathFolders(shader, 6)}\"");
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
        
        var logFile = Path.Combine(Paths.LogsFolder, "merge_into.log");
        File.Delete(logFile);
        using (var writer = new StreamWriter(logFile)) {
            foreach (var merge in merges) {
                writer.WriteLine($"checking merge\n{merge}");
                
                if (!guidDb.FilePathToGuid.TryGetValue(merge.FilePathFrom, out var guidFrom)) {
                    writer.WriteLine($"No guid for \"{Utility.ClampPathFolders(merge.FilePathFrom, 6)}\"");
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
                    writer.WriteLine($"No guidTo for \"{Utility.ClampPathFolders(merge.FilePathTo, 6)}\"");
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
                toReplace.Add(new GuidDatabaseMerge(guidFrom, guidTo));
                writer.WriteLine($"toReplace.Add:\n - {merge}\n - {new GuidDatabaseMerge(guidFrom, guidTo)}");
            }
            
            foreach (var merge in toReplace) {
                // find things that use the original guid
                writer.WriteLine($"{merge.GuidFrom} to {merge.GuidTo}");
                
                if (!guidDb.AssociatedFilePaths.TryGetValue(merge.GuidFrom, out var list)) {
                    writer.WriteLine($" - No associated file path");
                    continue;
                }
                
                foreach (var association in list) {
                    var shortPath = Utility.ClampPathFolders(association, 4);
                    writer.WriteLine($" - [associated] {shortPath}");
                }
            }
            
            foreach (var replace in additionalReplacements) {
                var existing = toReplace.FirstOrDefault(x => x.GuidFrom == replace.GuidTo);
                if (existing != null) {
                    var newReplace = new GuidDatabaseMerge(replace.GuidFrom, existing.GuidTo);
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
    
    public static HashSet<string> GetExclusionFiles(UnityGuid[] guids, GuidDatabase guidDb, RoslynDatabase typeDb) {
        var exclusionFiles = new HashSet<string>(1024);
        foreach (var guid in guids.Distinct()) {
            Console.WriteLine($"searching for {guid}:");
            
            if (guidDb.Assets.TryGetValue(guid, out var asset)) {
                // Console.WriteLine($" - for asset: {Utility.ClampPathFolders(asset.FilePath, 6)}");
                exclusionFiles.Add(asset.FilePath);
                continue;
            }
            
            if (guidDb.AssociatedFilePaths.TryGetValue(guid, out var paths)) {
                foreach (var path in paths) {
                    // Console.WriteLine($" - for path: {Utility.ClampPathFolders(path, 6)}");
                    exclusionFiles.Add(path);
                }
                continue;
            }
            
            // Console.WriteLine($" - no file in dbs");
        }
        
        return exclusionFiles;
    }
    public void WriteToDisk(string name) {
        File.Delete(name);

        using var writer = new StreamWriter(name);
        
        writer.WriteLine("Assets:");
        writer.WriteLine("---------------");
        foreach (var (guid, asset) in FullNameToFilePath) {
            var filePath = Utility.ClampPathFolders(asset, 6);
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
