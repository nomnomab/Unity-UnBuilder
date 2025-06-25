using System.Collections.Concurrent;
using AssetRipper.SourceGenerated;

namespace Nomnom;

/// <summary>
/// Stores all of the needed guids per relative file path for lookup.
/// </summary>
public record GuidDatabase {
    public required Dictionary<UnityGuid, AssetFile> Assets { get; set; }
    public required Dictionary<string, UnityGuid> FilePathToGuid { get; set; }
    public required Dictionary<UnityGuid, HashSet<string>> AssociatedFilePaths { get; set; }
    public required HashSet<UnityDllReference> DllReferences { get; set; }
    
    public static GuidDatabase Parse(string folderPath) {
        var db = new GuidDatabase() {
            Assets              = [],
            FilePathToGuid      = [],
            AssociatedFilePaths = [],
            DllReferences       = [],
        };
        
        var files                 = GetFiles(folderPath);
        var dbAssets              = new ConcurrentDictionary<UnityGuid, AssetFile>();
        var dbFilePathToGuid      = new ConcurrentDictionary<string, UnityGuid>();
        var dbAssociatedFilePaths = new ConcurrentDictionary<UnityGuid, ConcurrentBag<string>>();
        
        Parallel.ForEach(files, file => {
            var extension = Path.GetExtension(file);
            if (extension == null) return;

            var clampedFile = Utility.ClampPathFolders(file);
            Console.WriteLine($"Checking guids for {clampedFile}");
            
            switch (extension) {
                case ".meta": {
                    // parse meta file
                    var assetFile = file[..^".meta".Length];
                    
                    if (!dbFilePathToGuid.ContainsKey(assetFile)) {
                        var metaFile = UnityAssetTypes.ParseMetaFile(file);
                        if (metaFile == null) {
                            // Console.WriteLine($" - no metaFile");
                            return;
                        }
                        
                        dbFilePathToGuid.TryAdd(assetFile, metaFile.Guid);
                        addAssociatedGuid(metaFile.Guid, assetFile);
                        addAssociatedGuid(metaFile.Guid, file);
                        
                        // Console.WriteLine($" - {metaFile.Guid}::meta:\n - {assetFile}\n - {file}");
                    }
                }
                break;
                
                case ".mp3":
                case ".mp4":
                case ".ogg":
                case ".wav":
                case ".shader":
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tiff":
                case ".tga":
                case ".gif":
                case ".svg":
                break;
                
                default: {
                    // parse meta file
                    var metaFilePath = file + ".meta";
                    var metaFile = UnityAssetTypes.ParseMetaFile(metaFilePath);
                    
                    // needs a meta file for a guid
                    if (metaFile == null) {
                        // Console.WriteLine($" - no metaFile");
                        return;
                    }
                
                    var assetFile = UnityAssetTypes.ParseAssetFile(file);
                    if (assetFile == null) {
                        // Console.WriteLine($" - no assetFile");
                        return;
                    }
                    
                    // attach to asset file path + guid
                    dbAssets.TryAdd(metaFile.Guid, assetFile);
                    dbFilePathToGuid.TryAdd(assetFile.FilePath, metaFile.Guid);
                    addAssociatedGuid(metaFile.Guid, assetFile.FilePath);
                    addAssociatedGuid(metaFile.Guid, metaFilePath);
                    
                    // Console.WriteLine($" - {metaFile.Guid}::asset:\n - {assetFile}\n - {assetFile.FilePath}\n - {metaFilePath}");
                    
                    foreach (var obj in assetFile.Objects) {
                        foreach (var reference in obj.AssetReferences) {
                            addAssociatedGuid(reference.Guid, assetFile.FilePath);
                            // Console.WriteLine($"     - {reference}");
                        }
                    }
                }
                break;
            }
        });
        
        db.Assets              = dbAssets.ToDictionary();
        db.FilePathToGuid      = dbFilePathToGuid.ToDictionary();
        db.AssociatedFilePaths = dbAssociatedFilePaths.ToDictionary(
            x => x.Key,
            x => x.Value.ToHashSet()
        );
        
        return db;
        
        void addAssociatedGuid(UnityGuid guid, string filePath) {
            if (!dbAssociatedFilePaths.TryGetValue(guid, out ConcurrentBag<string>? value)) {
                dbAssociatedFilePaths.TryAdd(guid, [filePath]);
                return;
            }

            value.Add(filePath);
        }
    }
    
    public void AddAssociatedGuid(UnityGuid guid, string filePath) {
        if (!AssociatedFilePaths.TryGetValue(guid, out var associated)) {
            AssociatedFilePaths.Add(guid, [filePath]);
        } else {
            associated.Add(filePath);
        }
    }
    
    private static IEnumerable<string> GetFiles(string folderPath) {
        return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            // find meta and asset files
            .Where(x => {
                if (!Path.HasExtension(x)) {
                    return false;
                }
                
                var end = Path.GetExtension(x).ToLower();
                // return end == ".meta" || end == ".asset";
                return end != ".dll" && end != ".new";
            });
    }
    
    private static IEnumerable<string> GetBuiltInPackages(UnityPath unityPath) {
        // now scrub the built in packages list
        var builtInPath = unityPath.GetBuiltInPackagesPath();
        var folders     = Directory.GetDirectories(builtInPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var folder in folders) {
            foreach (var file in GetFiles(folder)) {
                yield return file;
            }
        }
    }
    
    /// <summary>
    /// Go through each file mapping and write its guid back to disk.
    /// </summary>
    public static IEnumerable<UnityGuid> ReplaceGuids(IEnumerable<GuidDatabaseMerge> merge, GuidDatabase[] databases) {
        var logPath = Path.Combine(Paths.LogsFolder, "replace_guids.log");
        File.Delete(logPath);
        using var writer = new StreamWriter(logPath);
        
        // need to:
        // 1. Get all guid associations
        // 2. Replace the guid in each assocated file
        // 3. Write back to disk
        //
        // probably write to a readonly file so the source can stay the same (?)
        
        // asset -> asset
        //      1. replace meta file guid
        //      2. replace old guid in all association with new guid
        
        // meta -> meta
        //      1. replace meta file guid
        //      2. replace old guid in all association with new guid
        
        // collect all guid changes per file to do one final replacement sweep
        var files  = new Dictionary<string, HashSet<GuidDatabaseMerge>>(256);
        var mainDb = databases[0];
        foreach (var m in merge) {
            if (!mainDb.AssociatedFilePaths.TryGetValue(m.GuidFrom, out var filePaths)) {
                // Console.WriteLine($"[not_found] {m.GuidFrom} with {m.GuidTo}");
                writer.WriteLine($"[not_found] {m.GuidFrom} with {m.GuidTo}");
                continue;
            }
            
            Console.WriteLine($"Replacing {m.GuidFrom} with {m.GuidTo} across {filePaths.Count} files");
            foreach (var filePath in filePaths) {
                // Console.WriteLine($" - {filePath}");
                writer.WriteLine($" - {filePath}");
                if (!files.TryGetValue(filePath, out var list)) {
                    files.Add(filePath, []);
                    list = files[filePath];
                }
                
                writer.WriteLine($"[added] {m} for {Utility.ClampPathFolders(filePath)}");
                list.Add(m);
            }
        }
        
        foreach (var (file, list) in files) {
            var clamped = Utility.ClampPathFolders(file);
            // Console.WriteLine($"[file] {clamped}:");
            writer.WriteLine($"[file] {clamped}:");
            
            var extension = Path.GetExtension(file);
            switch (extension) {
                case ".meta": {
                    // meta file
                    ReplaceGuidInFile(file, list, writer);
                    // yield return file;
                    
                    foreach (var entry in list) {
                        yield return entry.GuidFrom;
                    }
                }
                break;
                
                default: {
                    // any other file
                    if (!mainDb.FilePathToGuid.TryGetValue(file, out var guid)) {
                        // Console.WriteLine($" - no file");
                        writer.WriteLine($" - no file");
                        continue;
                    }
                    
                    var db = databases.Where(x => x.Assets.ContainsKey(guid))
                        .FirstOrDefault();
                    if (db == null) {
                        continue;
                    }
                    
                    var asset = db.Assets[guid];
                    if (asset == null) {
                        // edge case extensions
                        if (extension == ".shader") {
                            ReplaceGuidInFile(file, list, writer);
                            
                            foreach (var entry in list) {
                                yield return entry.GuidFrom;
                            }
                            
                            continue;
                        }
                        
                        // Console.WriteLine($" - no asset for {guid}");
                        writer.WriteLine($" - no asset for {guid}");
                        continue;
                    }
                    
                    var first = asset.Objects.FirstOrDefault();
                    if (first == null) continue;
                    
                    switch (first.ClassId) {
                        case ClassIDType.MonoScript:
                        case ClassIDType.AudioClip:
                        case ClassIDType.MovieTexture:
                        case ClassIDType.TextAsset:
                        case ClassIDType.Texture2D:
                        case ClassIDType.Texture2DArray:
                        case ClassIDType.Texture3D:
                        case ClassIDType.VideoClip_327:
                        case ClassIDType.VideoClip_329:
                        case ClassIDType.BaseVideoTexture:
                        
                        // Console.WriteLine($" - cannot handle \"{first.ClassId}\"");
                        writer.WriteLine($" - cannot handle \"{first.ClassId}\"");
                        
                        continue;
                    }
                    
                    ReplaceGuidInFile(file, list, writer);
                    // yield return asset.FilePath;
                    
                    foreach (var entry in list) {
                        yield return entry.GuidFrom;
                    }
                }
                break;
            }
        }
    }
    
    private static void ReplaceGuidInFile(string filePath, IEnumerable<GuidDatabaseMerge> guids, StreamWriter writer) {
        File.Delete($"{filePath}.new");
        
        // var contents = File.ReadAllText(filePath);
        var count    = 0;
        
        // todo: do this line by line instead!
        var lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i++) {
            var line = lines[i];
            
            // check if this contains a guid
            foreach (var guid in guids) {
                if (line.Contains(guid.GuidFrom.Value)) {
                    if (guid.FileIdTo != null) {
                        writer.WriteLine($" - replaced {guid.GuidFrom.Value} with {guid.FileIdTo}:{guid.GuidTo.Value}:{guid.FileTypeTo}");
                        
                        // replace the whole line
                        lines[i] = UnityAssetTypes.AssetReferencePattern.Replace(line, $"{{fileID: {guid.FileIdTo?.Value}, guid: {guid.GuidTo}, type: {guid.FileTypeTo?.Value}}}");
                    } else {
                        writer.WriteLine($" - replaced {guid.GuidFrom.Value} with {guid.GuidTo.Value}");
                        
                        // replace just the guid
                        lines[i] = line.Replace(guid.GuidFrom.Value, guid.GuidTo.Value);
                    }
                    
                    count++;
                }
            }
        }
        
        // foreach (var pair in guids) {
        //     // Console.WriteLine($" - replaced {pair.GuidFrom.Value} with {pair.GuidTo.Value}");
        //     writer.WriteLine($" - replaced {pair.GuidFrom.Value} with {pair.GuidTo.Value}");
            
        //     if (pair.FileIdTo != null) {
        //         // replace the whole line
        //         contents = contents.Replace(pair.GuidFrom.Value, pair.GuidTo.Value);
        //     } else {
        //         // replace just the guid
        //         contents = contents.Replace(pair.GuidFrom.Value, pair.GuidTo.Value);
        //     }
        //     count++;
        // }
        
        var contents = string.Join('\n', lines);
        if (!Utility.IsRunningTests) {
            File.WriteAllText($"{filePath}.new", contents);
        }
        
        Console.WriteLine($"{Utility.ClampPathFolders(filePath)} replaced {count} guids");
    }
    
    public void WriteToDisk(string name) {
        var logPath = Path.Combine(Paths.LogsFolder, name);
        File.Delete(logPath);

        using var writer = new StreamWriter(logPath);
        
        writer.WriteLine("Assets:");
        writer.WriteLine("---------------");
        foreach (var (guid, asset) in Assets) {
            var filePath = Utility.ClampPathFolders(asset.FilePath);
            writer.WriteLine($"[{guid}] {filePath}");
            
            foreach (var obj in asset.Objects) {
                writer.WriteLine($" - [{obj}]");
                
                foreach (var assetRef in obj.AssetReferences) {
                    writer.WriteLine($"   - [asset_ref] {assetRef}");
                }
                
                foreach (var nested in obj.NestedReferences) {
                    writer.WriteLine($"   - [nested   ] {nested}");
                }
            }
        }
        
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteLine("Associations:");
        writer.WriteLine("---------------");
        foreach (var (guid, paths) in AssociatedFilePaths) {
            var isAnAsset = Assets.ContainsKey(guid);
            if (isAnAsset) {
                writer.WriteLine($"[asset] {guid}");
            } else {
                writer.WriteLine($"[other] {guid}");
            }
            foreach (var path in paths) {
                var smallPath = Utility.ClampPathFolders(path);
                writer.WriteLine($" - {smallPath}");
            }
        }
    }
}

public record GuidDatabaseMerge(UnityGuid GuidFrom, UnityGuid GuidTo, UnityFileId? FileIdTo, UnityFileType? FileTypeTo);
