using System.Text.Json;
using System.Text.RegularExpressions;
using AssetRipper.SourceGenerated;

namespace Nomnom;

public static class FixAddressables {
    static IEnumerable<SavedResourceLocationMap> GetMaps(ToolSettings settings) {
        var settingsFolder = settings.GetSettingsFolder();
        var mapsFolder     = Path.Combine(settingsFolder, "Addressables");
        if (!Directory.Exists(mapsFolder)) {
            yield break;
        }
        
        // each json in the maps folder is a new table
        var files = Directory.GetFiles(mapsFolder, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files) {
            var json = File.ReadAllText(file);
            var map  = JsonSerializer.Deserialize<SavedResourceLocationMap>(json);
            if (map == null) continue;
            
            Console.WriteLine($"Found Addressables map: {map.LocatorId}");
            
            yield return map;
        }
    }
    
    public static async Task InstallAddressables(ToolSettings settings, GuidDatabase guidDatabase) {
        FixHashes(settings);
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var file        = Utility.CopyOverScript(projectPath, "InstallAddressables", x => {
            var maps  = GetMaps(settings);
            var paths = new HashSet<string>();
            foreach (var map in maps) {
                foreach (var (id, value) in map.Keys) {
                    if (Path.HasExtension(value.FileName)) {
                        paths.Add($"\"{value.FileName}\",");
                    }
                }
            }
            
            return x.Replace("#_PATHS_#", string.Join('\n', paths));
        });
        
        await UnityCLI.OpenProjectHidden("Installing Addressables", settings.GetUnityPath(), true, projectPath,
            "-executeMethod Nomnom.InstallAddressables.OnLoad"
        );
        
        FixAssets(settings, guidDatabase);
        
        File.Delete(file);
    }
    
    public static void FixHashes(ToolSettings settings) {
        Console.WriteLine($"Fixing Addressables hashes...");
        
        // var keys = new Dictionary<string, ResourceKey>();
        var bundleMap = new Dictionary<string, string>();
        foreach (var map in GetMaps(settings)) {
            // foreach (var key in map.Keys) {
            //     // keys.TryAdd(key.Key, key.Value);
            // }
            
            foreach (var value in map.Values) {
                if (value.Value is ResourceData data) {
                    var bundleName = data.GetBundleName();
                    if (bundleName != null) {
                        bundleMap.Add(bundleName, data.PrimaryKey);
                    }
                }
            }
        }
        
        Console.WriteLine($"bundles:");
        foreach (var (key, value) in bundleMap) {
            Console.WriteLine($"[{key}] {value}");
        }
        
        var assetsFolder = Path.Combine(
            settings.ExtractData.GetProjectPath(),
            "Assets"
        );
        
        var folders = Directory.GetDirectories(assetsFolder, "*", SearchOption.TopDirectoryOnly);
        foreach (var folder in folders) {
            var name = Path.GetFileName(folder);
            Console.WriteLine($"Checking {name}...");
            
            var foundReplacement = bundleMap.FirstOrDefault(x => x.Key.Contains(name));
            if (foundReplacement.Value == null) continue;
            
            var replacement = foundReplacement.Value;
            // if (!bundleMap.TryGetValue(name, out var replacement)) {
            //     continue;
            // }
            replacement = Path.GetFileNameWithoutExtension(replacement);
            
            // move the folder
            var newPath = Path.GetDirectoryName(folder)!;
            newPath = Path.Combine(newPath, replacement);
            if (!Utility.IsRunningTests) {
                Directory.Move(folder, newPath);
            }
            
            Console.WriteLine($" - replacing with {replacement}");

            // also move the meta file for the folder
            var metaFile = folder + ".meta";
            if (File.Exists(metaFile) && !Utility.IsRunningTests) {
                newPath += ".meta";
                File.Move(metaFile, newPath, true);
            }
        }
        
        var files = Directory.GetFiles(assetsFolder, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var file in files) {
            var name = Path.GetFileNameWithoutExtension(file);
            Console.WriteLine($"Checking {name}...");
            
            var foundReplacement = bundleMap.FirstOrDefault(x => x.Key.Contains(name));
            if (foundReplacement.Value == null) continue;
            
            var replacement = foundReplacement.Value;
            // if (!bundleMap.TryGetValue(name, out var replacement)) {
            //     continue;
            // }
            
            // move the file
            var newPath = Path.GetDirectoryName(file)!;
            var extension = Path.GetExtension(file);
            replacement += extension;
            newPath = Path.Combine(newPath, replacement);
            if (!Utility.IsRunningTests) {
                File.Move(file, newPath);
            }
            
            Console.WriteLine($" - replacing with {replacement}");

            // also move the meta file for the folder
            var metaFile = file + ".meta";
            if (File.Exists(metaFile) && !Utility.IsRunningTests) {
                newPath += ".meta";
                File.Move(metaFile, newPath, true);
            }
        }
    }
    
    public static void FixAssets(ToolSettings settings, GuidDatabase guidDatabase) {
        Console.WriteLine($"Fixing Addressables assets...");
        
        var projectPath = settings.ExtractData.GetProjectPath();
        var assetsFolder = Path.Combine(
            projectPath,
            "Assets"
        );
        
        // todo: replace m_AssetGUID: guid with matching guid
        // todo: get guid from m_AssetGUID -> asset name -> guid found
        
        // get all the id -> guid relations
        var idToGuidLookup = new Dictionary<string, UnityGuid>(512);
        foreach (var map in GetMaps(settings)) {
            foreach (var (id, value) in map.Keys) {
                var file = Path.Combine(projectPath, value.FileName);
                if (File.Exists(file)) {
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
                    Console.WriteLine($"[{id}] {guid} for: {file}");
                } else {
                    Console.WriteLine($"file does not exist at {file}");
                }
            }
        }
        // foreach (var map in GetMaps(settings)) {
        //     foreach (var (id, value) in map.Keys) {
        //         // does this file exist?
        //         // it can either exist all in the root of the project (no duplicates)
        //         // or in sub-folders (possible duplicates)
        //         var file = Path.Combine(assetsFolder, value.FileName);
        //         if (File.Exists(file)) {
        //             // take the file's guid
        //             file = file.Replace(
        //                 projectPath,
        //                 settings.ExtractData.Config.ProjectRootPath
        //             );
        //             if (!guidDatabase.FilePathToGuid.TryGetValue(file, out var guid)) {
        //                 continue;
        //             }
                    
        //             // id now maps to guid
        //             idToGuidLookup[id] = guid;
        //             Console.WriteLine($"[{id}] {guid} for: {file}");
        //             continue;
        //         }
                
        //         // check in subfolder since it doesn't exist in the root
        //         string? folderToCheck = Path.GetExtension(value.FileName) switch {
        //             ".anim"       => "AnimationClip",
        //             ".controller" => "Controller",
        //             // ".ogg" or 
        //             // ".mp3" or
        //             // ".wav"        => "AudioClip",
        //             // ".ttf" or
        //             // ".otf"        => "Font",
        //             ".prefab"     => "GameObject",
        //             ".mat"        => "Material",
        //             ".asset"      => "MonoBehaviour",
        //             ".unity"      => "Scenes",
        //             ".shader"     => "Shader",
        //             _ => null
        //         };
                
        //         string[] files;
        //         if (folderToCheck != null) {
        //             var folderPath = Path.Combine(assetsFolder, folderToCheck);
        //             files          = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        //         } else {
        //             files          = Directory.GetFiles(assetsFolder, "*.*", SearchOption.AllDirectories);
        //         }
                
        //         var foundFiles = files.Where(x => Path.GetFileName(x) == value.FileName)
        //             .ToArray();
                    
        //         // found the file
        //         if (foundFiles.Length > 0) {
        //             // pick the first one and warn
        //             Console.WriteLine($"[{id}] has duplicates:\n{string.Join('\n', foundFiles)}");
                    
        //             var wasFound = false;
        //             foreach (var found in foundFiles.Select(x => {
        //                 return x.Replace(
        //                     settings.ExtractData.GetProjectPath(),
        //                     settings.ExtractData.Config.ProjectRootPath
        //                 );
        //             })) {
        //                 if (!guidDatabase.FilePathToGuid.TryGetValue(found, out var foundGuid)) {
        //                     Console.WriteLine($" - no guid for {found} in:\n{string.Join("\n", guidDatabase.FilePathToGuid.Keys)}");
        //                     continue;
        //                 }
                        
        //                 idToGuidLookup[id] = foundGuid;
        //                 Console.WriteLine($"[{id}] {foundGuid} for: {found}");
        //                 wasFound = true;
        //                 break;
        //             }
                    
        //             if (wasFound) {
        //                 Console.WriteLine(" - was found");
        //                 continue;
        //             }
        //         }

        //         Console.WriteLine($"[{id}] does not exist: {file}");
        //         continue;
        //     }
        // }
        
        Console.WriteLine($"Found {idToGuidLookup.Count} ids to find and replace");
        
        if (idToGuidLookup.Count == 0) return;
        
        Console.WriteLine($"ids:");
        foreach (var (id, guid) in idToGuidLookup) {
            Console.WriteLine($"[{id}] {guid.Value}");
        }
        
        // now replace all files that use this m_AssetGUID
        var assetGuidRegex = new Regex(@"m_AssetGUID:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);
        foreach (var (guid, paths) in guidDatabase.AssociatedFilePaths) {
            foreach (var path in paths.Distinct()) {
                var outputPath = path.Replace(
                    settings.ExtractData.Config.ProjectRootPath,
                    projectPath
                );
                
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
                
                var text    = File.ReadAllText(path);
                if (!text.Contains("m_AssetGUID:")) {
                    continue;
                }
                
                Console.WriteLine($"Checking {path}...");
                
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
                    if (!Utility.IsRunningTests) {
                        Console.WriteLine($" - wrote to {outputPath}");
                        File.WriteAllLines(outputPath, lines.Append("\n"));
                    }
                }
            }
        }
    }
    
    class SavedResourceLocationMap {
        public string LocatorId { get; set; }
        public Dictionary<string, ResourceKey> Keys { get; set; } = new();
        public Dictionary<string, ResourceValue> Values { get; set; } = new();
    }

    class ResourceKey {
        // public string Id { get; set; }
        public string FileName { get; set; }
    }

    class ResourceValue {
        public string InternalId { get; set; }
        public string PrimaryKey { get; set; }
        public string ResourceType { get; set; }
    }

    class ResourceData: ResourceValue {
        public object Data { get; set; }
        
        public string? GetBundleName() {
            var type       = Data.GetType();
            var bundleName = type.GetField("BundleName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?
                .GetValue(Data);
            return (string?)bundleName;
        }
    }
}
