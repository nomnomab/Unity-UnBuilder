using System.Text.RegularExpressions;
using AssetRipper.SourceGenerated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nomnom;

public static class FixAddressables {
    static IEnumerable<SavedResourceLocationMap> GetMaps(ToolSettings settings) {
        var settingsFolder = settings.GetGameFolder();
        var mapsFolder     = Path.Combine(settingsFolder, "Addressables");
        if (!Directory.Exists(mapsFolder)) {
            yield break;
        }
        
        // each json in the maps folder is a new table
        var files = Directory.GetFiles(mapsFolder, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files) {
            var json = File.ReadAllText(file);
            var map  = JsonConvert.DeserializeObject<SavedResourceLocationMap>(json);
            if (map == null) continue;
            
            Console.WriteLine($"Found Addressables map: {map.LocatorId}");
            
            yield return map;
        }
    }
    
    public static async Task InstallAddressables(ToolSettings settings, GuidDatabase guidDatabase) {
        var maps  = GetMaps(settings).ToArray();
        
        // todo: add a way to properly assign the file key to each addressable
        FixAssets(maps, settings, guidDatabase, out var pathToKey);
        
        Console.WriteLine($"addressable path to keys:");
        foreach (var (path, key) in pathToKey) {
            Console.WriteLine($"[{path}] {key}");
        }
        
        FixHashes(maps, settings, pathToKey);
        EnsureAssetLocations(maps, settings);
        
        // initialize the addressables settings
        var projectPath = settings.ExtractData.GetProjectPath();
        var file        = Utility.CopyOverScript(projectPath, "InstallAddressables", x => {
            var paths = new HashSet<string>(512);
            foreach (var map in maps) {
                foreach (var (id, value) in map.Keys) {
                    if (Path.HasExtension(value.FileName)) {
                        paths.Add($"\"{value.FileName}\",");
                    }
                }
            }
            
            var keys = pathToKey.Select(x => $"(@\"{x.Key.Replace('\\', '/')}\", @\"{x.Value}\"),");
            var contents = x
                .Replace("#_PATHS_#", string.Join('\n', paths))
                .Replace("#_KEYS_#" , string.Join('\n', keys));
            
            Console.WriteLine($"contents:\n{contents}");
            return contents;
        });
        
        await UnityCLI.OpenProjectHidden("Installing Addressables", settings.GetUnityPath(), true, projectPath,
            "-executeMethod Nomnom.InstallAddressables.OnLoad"
        );
        
        File.Delete(file);
    }
    
    /// <summary>
    /// Fixes file and folder names that are obfuscated as the
    /// Addressable hash.
    /// </summary>
    private static void FixHashes(SavedResourceLocationMap[] maps, ToolSettings settings, Dictionary<string, string> pathToKey) {
        Console.WriteLine($"Fixing Addressables hashes...");
        
        var keyMap = new Dictionary<string, string>();
        // fetch the id -> filePath conversion
        foreach (var map in maps) {
            foreach (var (key, value) in map.Keys) {
                var newKey = Path.GetFileName(value.FileName);
                keyMap.TryAdd(key, newKey);
            }
        }
        
        var projectPath  = settings.ExtractData.GetProjectPath();
        var assetsFolder = Path.Combine(
            projectPath,
            "Assets"
        );
        
        var folders = Directory.GetDirectories(assetsFolder, "*", SearchOption.AllDirectories);
        var files   = Directory.GetFiles(assetsFolder, "*.*", SearchOption.AllDirectories);
        
        foreach (var map in maps) {
            foreach (var folder in folders) {
                if (!Directory.Exists(folder)) continue;
                
                var folderName = Path.GetFileName(folder);
                if (!map.Keys.TryGetValue(folderName, out var keyValue)) {
                    continue;
                }
                
                var fileAssetPath = keyValue.FileName;
                var newName       = fileAssetPath;
                if (Path.HasExtension(fileAssetPath)) {
                    newName = Path.GetFileNameWithoutExtension(fileAssetPath);
                }
                
                var newPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(folder)!,
                    newName
                ));
                
                Console.WriteLine($"[folder {folderName}]\n{folder}\n{newPath}");
                Directory.Move(folder, newPath);
                
                var fileMeta = folder + ".meta";
                if (File.Exists(fileMeta)) {
                    Console.WriteLine($"[folder {folderName}]\n{fileMeta}\n{newPath + ".meta"}");
                    
                    File.Move(fileMeta, newPath + ".meta");
                }
                
                fileAssetPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(newPath)!,
                    newName
                ));
                
                fileAssetPath = fileAssetPath.Replace(projectPath, string.Empty)[1..]
                    .Replace('\\', '/');
                
                pathToKey[fileAssetPath] = folderName;
            }
            
            foreach (var file in files) {
                if (!File.Exists(file)) continue;
                
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!map.Keys.TryGetValue(fileName, out var keyValue)) {
                    continue;
                }
                
                var extension     = Path.GetExtension(file);
                var fileAssetPath = keyValue.FileName;
                var newName       = fileAssetPath;
                if (Path.GetExtension(fileAssetPath) != extension) {
                    newName = Path.GetFileNameWithoutExtension(fileAssetPath);
                } else {
                    extension = null;
                }
                
                var dirName = Path.GetDirectoryName(file);
                // some weird cases can make a double Assets appear
                if (Path.GetFileName(dirName) == "Assets") {
                    dirName = Path.GetDirectoryName(dirName);
                }
                
                var newPath = Path.GetFullPath(Path.Combine(
                    dirName!,
                    newName + (extension ?? string.Empty)
                ));
                Console.WriteLine($"[file {fileName}]\n{file}\n{newPath}");
                
                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                File.Move(file, newPath);
                
                var fileMeta = file + ".meta";
                if (File.Exists(fileMeta)) {
                    Console.WriteLine($"[file {fileName}]\n{fileMeta}\n{newPath + ".meta"}");
                    
                    File.Move(fileMeta, newPath + ".meta");
                }
                
                fileAssetPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(newPath)!,
                    newName
                ));
                
                fileAssetPath = fileAssetPath.Replace(projectPath, string.Empty)[1..]
                    .Replace('\\', '/');
                
                pathToKey[fileAssetPath] = fileName;
            }
        }
        
        foreach (var folder in folders) {
            if (!Directory.Exists(folder)) continue;
            
            var folderName = Path.GetFileName(folder);
            if (keyMap.TryGetValue(folderName, out var name)) {
                var newName = name;
                if (Path.HasExtension(name)) {
                    newName = Path.GetFileNameWithoutExtension(name);
                }
                
                var newPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(folder)!,
                    newName
                ));
                Console.WriteLine($"[folder {folderName}]\n{folder}\n{newPath}");
                
                Directory.Move(folder, newPath);
                
                var fileMeta = folder + ".meta";
                if (File.Exists(fileMeta)) {
                    Console.WriteLine($"[folder {folderName}]\n{fileMeta}\n{newPath + ".meta"}");
                    
                    File.Move(fileMeta, newPath + ".meta");
                }
            }
        }
        
        foreach (var file in files) {
            if (!File.Exists(file)) continue;
            
            var fileName  = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);
            if (keyMap.TryGetValue(fileName, out var name)) {
                var newName = name;
                if (Path.GetExtension(name) != extension) {
                    newName = Path.GetFileNameWithoutExtension(name);
                } else {
                    extension = null;
                }
                
                var newPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(file)!,
                    newName + (extension ?? string.Empty)
                ));
                Console.WriteLine($"[file {fileName}]\n{file}\n{newPath}");
                
                File.Move(file, newPath);
                
                var fileMeta = file + ".meta";
                if (File.Exists(fileMeta)) {
                    Console.WriteLine($"[file {fileName}]\n{fileMeta}\n{newPath + ".meta"}");
                    
                    File.Move(fileMeta, newPath + ".meta");
                }
            }
        }
    }
    
    private static void EnsureAssetLocations(SavedResourceLocationMap[] maps, ToolSettings settings) {
        Console.WriteLine($"Fixing Addressables asset locations...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsFolder = Path.Combine(
            projectPath,
            "Assets"
        );
        
        foreach (var map in maps) {
            foreach (var fileName in map.Keys.SelectMany(x => x.Value.GetFileNames(projectPath, x.Key))) {
                Console.WriteLine($"checking {fileName}");
                
                var file = fileName;
                if (File.Exists(file)) continue;
                
                // file does not exist at this path, check the root
                var rootPath = Path.Combine(
                    assetsFolder,
                    Path.GetFileName(file)
                );
                
                if (!File.Exists(rootPath)) {
                    Console.WriteLine($"{rootPath} does not exist");
                    continue;
                }
                
                // file exists in root, move it
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.Move(rootPath, file);
                // yield return (rootPath, file);
                
                // if there is a meta file, move that too
                var metaPath = rootPath + ".meta";
                if (File.Exists(metaPath)) {
                    File.Move(metaPath, file + ".meta");
                    // yield return (metaPath, file + ".meta");
                }
                
                // is there an associated folder?
                if (Path.GetExtension(file) == ".unity") {
                    var folder = Path.Combine(
                        Path.GetDirectoryName(rootPath)!,
                        Path.GetFileNameWithoutExtension(rootPath)
                    );
                    
                    if (Directory.Exists(folder)) {
                        // move the folder
                        var newFolder = Path.Combine(
                            Path.GetDirectoryName(file)!,
                            Path.GetFileNameWithoutExtension(rootPath)
                        );
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(newFolder)!);
                        Directory.Move(folder, newFolder);
                        // yield return (folder, newFolder);
                        
                        // if there is a meta file, move that too
                        metaPath = folder + ".meta";
                        if (File.Exists(metaPath)) {
                            File.Move(metaPath, newFolder + ".meta");
                            // yield return (metaPath, newFolder + ".meta");
                        }
                    }
                }
            }
        }
    }
    
    private static void FixAssets(SavedResourceLocationMap[] maps, ToolSettings settings, GuidDatabase guidDatabase, out Dictionary<string, string> pathToKey) {
        Console.WriteLine($"Fixing Addressables assets...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        
        // get all the id -> guid relations
        var idToGuidLookup = new Dictionary<string, UnityGuid>(512);
        pathToKey          = new Dictionary<string, string>(512);
        
        foreach (var map in maps) {
            foreach (var (id, value) in map.Keys) {
                if (!map.Values.TryGetValue(id, out var assetValue)) continue;
                
                foreach (var fileName in value.GetFileNames(projectPath, id)) {
                    Console.WriteLine($"checking {fileName}");
                    
                    // var file = Path.Combine(projectPath, fileName);
                    var file = fileName;
                    if (!File.Exists(file)) {
                        if (!Path.HasExtension(file) && assetValue.ResourceType.StartsWith("UnityEngine.ResourceManagement.ResourceProviders.SceneInstance")) {
                            // is a scene asset, re-make the path
                            file = Path.GetFullPath(Path.Combine(
                                projectPath,
                                "Assets",
                                file + ".unity"
                            ));
                        } else {
                            Console.WriteLine($"file does not exist at {file}");
                            continue;
                        }
                    }
                    
                    // take the file's guid
                    file = Path.GetFullPath(file.Replace(
                        projectPath,
                        settings.ExtractData.Config.ProjectRootPath
                    ));
                    
                    if (!guidDatabase.FilePathToGuid.TryGetValue(file, out var guid)) {
                        Console.WriteLine($"no filePathToGuid for {file}");
                        continue;
                    }
                    
                    // id now maps to guid
                    idToGuidLookup[id] = guid;
                    
                    var assetPath = file.Replace(
                        settings.ExtractData.Config.ProjectRootPath,
                        string.Empty
                    )[1..];
                    pathToKey[assetPath]    = value.FileName;
                    Console.WriteLine($"[{id}] {guid} for: {file}");
                }
            }
        }
        
        Console.WriteLine($"Found {idToGuidLookup.Count} ids to find and replace");
        
        if (idToGuidLookup.Count == 0) return;
        
        Console.WriteLine($"ids:");
        foreach (var (id, guid) in idToGuidLookup) {
            Console.WriteLine($"[{id}] {guid.Value}");
        }
        
        // now replace all files that use this m_AssetGUID
        var assetGuidRegex = new Regex(@"m_AssetGUID:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);
        
        // todo: group this up better to run through a file once
        var allPaths = new HashSet<string>(512);
        foreach (var (guid, paths) in guidDatabase.AssociatedFilePaths) {
            foreach (var path in paths) {
                // var outputPath = Path.GetFullPath(path.Replace(
                //     settings.ExtractData.Config.ProjectRootPath,
                //     projectPath
                // ));
                
                if (!File.Exists(path)) continue;
                if (!Path.HasExtension(path)) continue;
                if (Path.GetExtension(path) 
                    is ".meta"
                    or ".cs"
                ) continue;
                
                if (!guidDatabase.Assets.TryGetValue(guid, out var asset)) {
                    continue;
                }
                switch (asset.Objects.FirstOrDefault()?.ClassId) {
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
                    continue;
                }
                
                var finalPath = Path.GetFullPath(path.Replace(
                    settings.ExtractData.Config.ProjectRootPath,
                    projectPath
                ));
                
                if (!File.Exists(finalPath)) continue;
                
                allPaths.Add(finalPath);
            }
        }
        
        var index = -1;
        foreach (var path in allPaths) {
            index++;
            
            var text = File.ReadAllText(path);
            if (!text.Contains("m_AssetGUID:")) {
                continue;
            }
            
            Console.WriteLine($"[{index}/{allPaths.Count}] checking {path}...");
            
            var lines = text.Split('\n');
            var changed = false;
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                if (!line.Contains("m_AssetGUID:")) continue;
                
                var assetGuid = assetGuidRegex.Match(line);
                if (!assetGuid.Success) continue;
                
                var assetGuidFound = assetGuid.Groups["guid"].Value;
                if (!idToGuidLookup.TryGetValue(assetGuidFound, out var newGuid)) {
                    Console.WriteLine($" - no idToGuidLookup for line: {line} ({assetGuidFound})");
                    continue;
                }
                
                if (assetGuidFound == newGuid.Value) {
                    Console.WriteLine($" - {assetGuidFound} == {newGuid.Value}");
                    continue;
                }
                
                Console.WriteLine($" - replaced \"{line}\" with {newGuid.Value}");
                line     = assetGuidRegex.Replace(line, $"m_AssetGUID: {newGuid.Value}");
                lines[i] = line;
                changed  = true;
            }
            
            if (changed) {
                // var outputPath = Path.GetFullPath(path.Replace(
                //     settings.ExtractData.Config.ProjectRootPath,
                //     projectPath
                // ));
                
                if (!Utility.IsRunningTests) {
                    Console.WriteLine($" - wrote to {path}");
                    File.WriteAllLines(path, lines.Append("\n"));
                }
            }
        }
        
        // foreach (var (guid, paths) in guidDatabase.AssociatedFilePaths) {
        //     var index = -1;
        //     foreach (var path in paths) {
        //         index++;
        //         var outputPath = Path.GetFullPath(path.Replace(
        //             settings.ExtractData.Config.ProjectRootPath,
        //             projectPath
        //         ));
                
        //         if (!File.Exists(path)) continue;
        //         if (!Path.HasExtension(path)) continue;
        //         if (Path.GetExtension(path) 
        //             is ".meta"
        //             or ".cs"
        //         ) continue;
        //         if (!guidDatabase.Assets.TryGetValue(guid, out var asset)) {
        //             continue;
        //         }
                
        //         switch (asset.Objects.FirstOrDefault()?.ClassId) {
        //             case ClassIDType.MonoScript:
        //             case ClassIDType.AudioClip:
        //             case ClassIDType.MovieTexture:
        //             case ClassIDType.TextAsset:
        //             case ClassIDType.Texture2D:
        //             case ClassIDType.Texture2DArray:
        //             case ClassIDType.Texture3D:
        //             case ClassIDType.VideoClip_327:
        //             case ClassIDType.VideoClip_329:
        //             case ClassIDType.BaseVideoTexture:
        //             continue;
        //         }
                
        //         var text    = File.ReadAllText(path);
        //         if (!text.Contains("m_AssetGUID:")) {
        //             continue;
        //         }
                
        //         Console.WriteLine($"[{index}/{paths.Count}] Checking {path}...");
                
        //         var lines = text.Split('\n');
        //         var changed = false;
        //         for (int i = 0; i < lines.Length; i++) {
        //             var line = lines[i];
        //             if (!line.Contains("m_AssetGUID:")) continue;
                    
        //             var assetGuid = assetGuidRegex.Match(line);
        //             if (!assetGuid.Success) continue;
                    
        //             var assetGuidFound = assetGuid.Groups["guid"].Value;
        //             if (!idToGuidLookup.TryGetValue(assetGuidFound, out var newGuid)) {
        //                 Console.WriteLine($" - no idToGuidLookup for line: {line} ({assetGuidFound})");
        //                 continue;
        //             }
                    
        //             if (assetGuidFound == newGuid.Value) {
        //                 Console.WriteLine($" - {assetGuidFound} == {newGuid.Value}");
        //                 continue;
        //             }
                    
        //             Console.WriteLine($" - replaced \"{line}\" with {newGuid.Value}");
        //             line     = assetGuidRegex.Replace(line, $"m_AssetGUID: {newGuid.Value}");
        //             lines[i] = line;
        //             changed  = true;
        //         }
                
        //         if (changed) {
        //             if (!Utility.IsRunningTests) {
        //                 Console.WriteLine($" - wrote to {outputPath}");
        //                 File.WriteAllLines(outputPath, lines.Append("\n"));
        //             }
        //         }
        //     }
        // }
    }
    
    class SavedResourceLocationMap {
        public string LocatorId { get; set; }
        public Dictionary<string, ResourceKey> Keys { get; set; } = new();
        public Dictionary<string, ResourceValue> Values { get; set; } = new();
    }

    class ResourceKey {
        // public string Id { get; set; }
        public string FileName { get; set; }
        
        public IEnumerable<string> GetFileNames(string rootPath, string id) {
            yield return Path.GetFullPath(Path.Combine(
                rootPath,
                FileName
            ));
            
            var folder    = Path.GetDirectoryName(FileName);
            var extension = Path.GetExtension(FileName) ?? string.Empty;
            
            if (folder == null) {
                yield return Path.GetFullPath(Path.Combine(
                    rootPath,
                    id + extension
                ));
            } else {
                yield return Path.GetFullPath(Path.Combine(
                    rootPath,
                    folder,
                    id + extension
                ));
            }
            
            yield return Path.GetFullPath(Path.Combine(
                rootPath,
                "Assets",
                Path.GetFileName(FileName)
            ));
            
            yield return Path.GetFullPath(Path.Combine(
                rootPath,
                "Assets",
                id + extension
            ));
        }
    }

    class ResourceValue {
        public string InternalId { get; set; }
        public string PrimaryKey { get; set; }
        public string ResourceType { get; set; }
        public JObject? Data { get; set; }
        
        public string? GetBundleName() {
            if (Data == null) {
                return null;
            }
            
            return Data["BundleName"]?.ToString();
        }
    }
}
