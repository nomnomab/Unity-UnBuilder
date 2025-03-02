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
        foreach (var script in allScripts) {
            Console.WriteLine($"Parsing \"{Utility.ClampPathFolders(script, 4)}\"...");
            
            try {
                await RoslynUtility.ParseTypesFromFile(script, namespacePartsCache, types);
            } catch {
                Console.WriteLine(" - Failed to parse");
                types.Clear();
                continue;
            }
            
            foreach (var type in types) {
                // Console.WriteLine($"Found type \"{type}\"");
                
                if (!db.FullNameToFilePath.TryAdd(type, script)) {
                    var existing = db.FullNameToFilePath[type];
                    // throw new Exception($"\"{type}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{script}\"");
                    Console.WriteLine($"\"{type}\" already exists in the database.\nexisting: \"{existing}\"\nattempted: \"{script}\"");
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
            foreach (var db in databases) {
                if (!db.FullNameToFilePath.TryGetValue(fullName, out var filePathTo)) {
                    continue;
                }
                
                if (filePathFrom == filePathTo) continue;

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
            if (!guidDb.FilePathToGuid.TryGetValue(merge.FilePathFrom, out var guidFrom)) continue;
            
            UnityGuid? guidTo = null;
            for (int i = 1; i < guidDatabases.Length; i++) {
                var db = guidDatabases[i];
                if (db.FilePathToGuid.TryGetValue(merge.FilePathTo, out guidTo)) {
                    break;
                }
            }
            
            if (guidTo == null) continue;
            if (guidFrom == guidTo) continue;
            
            if (!fromGuids.Add(guidFrom)) {
                // var existing = toReplace.First(x => x.GuidFrom == guidFrom);
                
                // if it is the same file name, just ignore this
                // can be from a different root project
                // todo: handle this better?
                continue;
                
                // throw new Exception($"Tried to add guid {guidFrom} again:\n - in list: {existing}\n - new: {merge}");
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
}

public record RoslynDatabaseMerge(RoslynDatabase DbFrom, RoslynDatabase DbTo, string FilePathFrom, string FilePathTo);
