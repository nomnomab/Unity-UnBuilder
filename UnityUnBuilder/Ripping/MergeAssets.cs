namespace Nomnom;

public static class MergeAssets {
    /// <summary>
    /// Attempts to merge 1:1 matching files to a singular file.
    /// Also makes sure to migrate the other file's guid to the original.
    /// </summary>
    public static IEnumerable<GuidDatabaseMerge> Merge(GuidDatabase guidDb, RoslynDatabase typeDb) {
        // shaders
        return MergeShaders(guidDb, typeDb);
    }
    
    private static IEnumerable<GuidDatabaseMerge> MergeShaders(GuidDatabase guidDb, RoslynDatabase typeDb) {
        foreach (var (name, paths) in typeDb.ShaderNameToFilePaths) {
            Console.WriteLine($"shader: {name}");
            
            if (paths.Count == 0) continue;
            
            if (!guidDb.FilePathToGuid.TryGetValue(paths[0], out var mainGuid)) {
                continue;
            }
            
            var text      = File.ReadAllText(paths[0]);
            var sameFiles = new List<string>();
            for (int i = 1; i < paths.Count; i++) {
                var path = paths[i];
                Console.WriteLine($" - checking: {Utility.ClampPathFolders(path)}");
                
                var checkText = File.ReadAllText(paths[i]);
                if (text == checkText) {
                    Console.WriteLine($"   - same");
                    sameFiles.Add(path);
                }
            }
            
            foreach (var same in sameFiles){
                if (!guidDb.FilePathToGuid.TryGetValue(same, out var guid)) {
                    continue;
                }
                
                Console.WriteLine($" - guid: {guid} @ {Utility.ClampPathFolders(same)}");
                // guidDb.AddAssociatedGuid(guid, same);
                
                yield return new GuidDatabaseMerge(guid, mainGuid, null, null);
            }
        }
    }
}
