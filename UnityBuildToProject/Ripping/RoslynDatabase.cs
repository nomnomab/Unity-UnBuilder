
namespace Nomnom;

public record RoslynDatabase {
    public required Dictionary<string, string> FullNameToFilePath { get; set; }
    
    public static async Task<RoslynDatabase> Parse(string folderPath) {
        var db = new RoslynDatabase() {
            FullNameToFilePath = []
        };
        
        var allScripts          = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories)
            .Where(x => !Path.GetFileName(x).StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes"))
            .Where(x => !x.EndsWith(".gen.cs"));
        var namespacePartsCache = new List<string>(capacity: 128);
        var types               = new List<string>(capacity: 1024);
        
        // todo: split into multiple tasks
        foreach (var script in allScripts) {
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(script, 6)}\"...");
            
            try {
                await RoslynUtility.ParseTypesFromFile(script, namespacePartsCache, types);
            } catch {
                Console.WriteLine($"Failed to parse \"{Utility.ClampPathFolders(script, 6)}\"");
                types.Clear();
                continue;
            }
            
            // if (types.Count == 0) {
            //     Console.WriteLine($" - no types");
            // }
            
            foreach (var type in types) {
                // Console.WriteLine($" - found type \"{type}\"");
                
                if (!db.FullNameToFilePath.TryAdd(type, script)) {
                    var existing = db.FullNameToFilePath[type];
                    // throw new Exception($"\"{type}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{script}\"");
                    Console.WriteLine($" - \"{type}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{script}\"");
                }
            }
            types.Clear();
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
    
    public static void MergeInto(GuidDatabase[] guidDatabases, RoslynDatabase[] typeDatabases) {
        var guidDb = guidDatabases[0];
        var typeDb = typeDatabases[0];
        
        var merges    = typeDb.GetMergeUnion(typeDatabases[1..]);
        var toReplace = new HashSet<GuidDatabaseMerge>(capacity: 512);
        var fromGuids = new HashSet<UnityGuid>(capacity: 1024);
        
        foreach (var merge in merges) {
            // Console.WriteLine($"checking merge\n{merge}");
            
            if (!guidDb.FilePathToGuid.TryGetValue(merge.FilePathFrom, out var guidFrom)) {
                // Console.WriteLine($"No guid for \"{Utility.ClampPathFolders(merge.FilePathFrom, 6)}\"");
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
                // Console.WriteLine($"No guidTo for \"{Utility.ClampPathFolders(merge.FilePathTo, 6)}\"");
                continue;
            }
            if (guidFrom == guidTo) continue;
            
            if (!fromGuids.Add(guidFrom)) {
                // if it is the same file name, just ignore this
                // can be from a different root project
                // todo: handle this better?
                continue;
            }
            
            // override!
            toReplace.Add(new GuidDatabaseMerge(guidFrom, guidTo));
            Console.WriteLine($"toReplace.Add:\n - {merge}\n - {new GuidDatabaseMerge(guidFrom, guidTo)}");
        }
        
        // debug
        var logFile = Path.Combine(Paths.LogsFolder, "write_log.log");
        File.Delete(logFile);
        using (var writer = new StreamWriter(logFile)) {
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
        }
        
        Console.WriteLine($"{toReplace.Count} to replace");
        GuidDatabase.ReplaceGuids(toReplace, guidDatabases);
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
    }
}

public record RoslynDatabaseMerge(RoslynDatabase DbFrom, RoslynDatabase DbTo, string FilePathFrom, string FilePathTo);
