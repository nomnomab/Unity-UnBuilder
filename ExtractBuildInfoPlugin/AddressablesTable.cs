using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace ExtractBuildInfoPlugin {
    public sealed class AddressablesTable {
        private static readonly string OutputFolder = Path.Combine(
            Path.Combine(
                Application.dataPath,
                ".."
            ), 
            "extract_data"
        );
        
        /// <summary>
        /// Extracts the resource maps in a minimal data format.
        /// This can be packaged with its game setting folder.
        /// </summary>
        public static void Extract() {
            // wait for Addressables to finish loading
            Addressables.InitializeAsync()
                .WaitForCompletion();

            Directory.CreateDirectory(OutputFolder);

            var maps = ExtractResourceMaps();
            WriteResourceMaps(maps);
        }

        private static IEnumerable<ResourceLocationMap> ExtractResourceMaps() {
            foreach (var locator in Addressables.ResourceLocators) {
                if (locator is not ResourceLocationMap map) continue;
                yield return map;
            }
        }

        private static void WriteResourceMaps(IEnumerable<ResourceLocationMap> maps) {
            // var foundFileNames = new HashSet<string>();
            foreach (var map in maps) {
                if (map is null) continue;
                
                Console.WriteLine($"found map: {map.LocatorId}");
                
                // convert to a smaller data version
                var savedMapPath = Path.Combine(OutputFolder, map.LocatorId + ".json");
                var savedMap = new SavedResourceLocationMap {
                    LocatorId = map.LocatorId,
                };

                // save keys
                ResourceKey previousEntry = null;
                foreach (var key in map.Keys) {
                    if (key == null) continue;
                    
                    Console.WriteLine($"found key: {key}");
                    // Console.WriteLine(JsonConvert.SerializeObject(key, Formatting.Indented));
                    
                    var id = key.ToString();
                    // var fileName = Path.GetFileName(id);
                    
                    var entry = new ResourceKey {
                        // Id = id,
                        FileName = id,
                    };
                    
                    // a 32 length string indicates an id
                    // the previous entry will be what it maps to
                    if (id.Length != 32) {
                        previousEntry = entry;
                        continue;
                    }

                    if (previousEntry == null) {
                        throw new Exception("No previous entry for id");
                    }
                    
                    if (savedMap.Keys.ContainsKey(id)) {
                        continue;
                    }
                    
                    Console.WriteLine($" - new file: {id} for {previousEntry.FileName}");
                    
                    entry.FileName = previousEntry.FileName;
                    savedMap.Keys.Add(id, entry);
                    previousEntry = entry;
                }
                
                // save locations
                foreach (var location in map.Locations) {
                    Console.WriteLine($"location: {location.Key}");
                    
                    foreach (var resourceLocation in location.Value) {
                        if (resourceLocation == null) continue;
                        
                        // Console.WriteLine(JsonConvert.SerializeObject(resourceLocation, Formatting.Indented));

                        var id = resourceLocation.InternalId;
                        if (id.EndsWith(".bundle")) {
                            var last = id.LastIndexOf("StreamingAssets", StringComparison.Ordinal);
                            id = id[last..];
                        }
                        
                        if (savedMap.Values.ContainsKey(id)) {
                            continue;
                        }

                        ResourceValue value;
                        if (resourceLocation.Data != null) {
                            value = new ResourceData {
                                InternalId = id,
                                PrimaryKey = resourceLocation.PrimaryKey,
                                ResourceType = resourceLocation.ResourceType,
                                Data = resourceLocation.Data,
                            };
                        } else {
                            value = new ResourceValue {
                                InternalId = id,
                                PrimaryKey = resourceLocation.PrimaryKey,
                                ResourceType = resourceLocation.ResourceType,
                            };
                        }
                        savedMap.Values.Add(id, value);
                    }
                }
                
                var json = JsonConvert.SerializeObject(savedMap, Formatting.Indented);
                File.WriteAllText(savedMapPath, json);
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
        public Type ResourceType { get; set; }
    }

    class ResourceData: ResourceValue {
        public object Data { get; set; }
    }
}
