using System.Text.RegularExpressions;

namespace Nomnom;

// utilities
public static partial class UnityAssetTypes {
    public static MetaFile? ParseMetaFile(string path) {
        if (!File.Exists(path)) {
            return null;
        }
        
        // simply look for a guid field
        // guid: 7ee5d3658460dde439b28eddc73c89ef
        
        using var reader = new StreamReader(path);
        while (reader.Peek() >= 0) {
            // read lines
            var line = reader.ReadLine();
            if (line == null) continue;
            
            // parse guid
            var guid = ParseGuid(line);
            if (guid != null) {
                return new MetaFile() {
                    FilePath = Path.GetFullPath(path),
                    Guid     = guid
                };
            }
        }
        
        return null;
    }
    
    public static AssetFile? ParseAssetFile(string path) {
        if (!File.Exists(path)) {
            return null;
        }
        
        // assets can store many references to other files
        // and to ids within itself, such as with subassets
        // or scene child objects
        //
        // an asset reference:
        // m_Script: {fileID: 11500000, guid: 60d655b07d2cd0339744b343f35ac324, type: 3}
        //
        // a sub-asset:
        // --- !u!114 &114811119258149997
        //
        // a sub-asset reference:
        // {fileID: 114811119258149997}
        
        var mainAsset = AssetFile.Default;
        mainAsset.FilePath = Path.GetFullPath(path);
        
        using var reader = new StreamReader(path);
        
        UnityObjectDefinition? currentObj = null;
        while (reader.Peek() >= 0) {
            // read lines
            var line = reader.ReadLine();
            if (line == null) continue;
            
            // parse object
            var newObj = ParseObjectDefinition(line);
            if (newObj != null) {
                currentObj = newObj;
                mainAsset.Objects.Add(newObj);
                continue;
            }
            
            if (currentObj != null) {
                // asset ref
                var assetRef = ParseAssetReference(line);
                if (assetRef != null) {
                    currentObj.AssetReferences.Add(assetRef);
                    continue;
                }
                
                // parse subasset ref
                var fileRef = ParseFileId(line);
                if (fileRef != null) {
                    currentObj.NestedReferences.Add(fileRef);
                    continue;
                }
            }
        }
        
        return mainAsset;
    }
    
    private static UnityGuid? ParseGuid(string line) {
        var guid = GuidPattern.Match(line);
        if (guid != null && guid.Success) {
            var value = guid.Groups["guid"].Value;
            if (string.IsNullOrEmpty(value)) {
                return null;
            }
            
            return new UnityGuid(value);
        }
        
        return null;
    }
    
    private static UnityFileId? ParseFileId(string line) {
        var fileId = AssetSubReferencePattern.Match(line);
        if (fileId != null && fileId.Success) {
            var value = fileId.Groups["fileId"].Value;
            if (string.IsNullOrEmpty(value)) {
                return null;
            }
            
            // fileId of 0 references self
            if (value[0] == '0') {
                return null;
            }
            
            return new UnityFileId(value);
        }
        
        return null;
    }
    
    private static UnityObjectDefinition? ParseObjectDefinition(string line) {
        var match = FileIdReferencePattern.Match(line);
        if (match != null && match.Success) {
            var classId = match.Groups["classId"].Value;
            var fileId  = match.Groups["fileId"].Value;
            
            if (string.IsNullOrEmpty(classId) || string.IsNullOrEmpty(fileId)) {
                return null;
            }
            
            if (!int.TryParse(classId, out var classIdInt)) {
                return null;
            }
            
            var obj = new UnityObjectDefinition() {
                ClassId          = (UnityClassId)classIdInt,
                FileId           = new UnityFileId(fileId),
                AssetReferences  = [],
                NestedReferences = [],
            };
            
            return obj;
        }
        
        return null;
    }
    
    private static UnityAssetReference? ParseAssetReference(string line) {
        var match = AssetReferencePattern.Match(line);
        if (match != null && match.Success) {
            var guid   = match.Groups["guid"].Value;
            var fileId = match.Groups["fileId"].Value;
            var type   = match.Groups["type"].Value;
            
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(type)) {
                return null;
            }
            
            if (!int.TryParse(type, out var typeInt)) {
                return null;
            }
            
            return new UnityAssetReference() {
                Guid   = new UnityGuid(guid),
                FileId = new UnityFileId(fileId),
                Type   = new UnityFileType(typeInt)
            };
        }
        
        return null;
    }
    
    private readonly static Regex GuidPattern = GetGuidRegex();
    private readonly static Regex FileIdReferencePattern = GetFileIdReferenceRegex();
    private readonly static Regex AssetReferencePattern = GetAssetReferenceRegex();
    private readonly static Regex AssetSubReferencePattern = GetAssetSubReferenceRegex();
    
    [GeneratedRegex(@"guid:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled)]
    private static partial Regex GetGuidRegex();
    
    [GeneratedRegex(@"--- !u!(?<classId>\w+)\s&(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled)]
    private static partial Regex GetFileIdReferenceRegex();
    
    [GeneratedRegex(@"{fileID:\s(?<fileId>[0-9A-Za-z]+),\s*guid:\s(?<guid>[0-9A-Za-z]+),\s*type:\s(?<type>[0-9]+)}", RegexOptions.Compiled)]
    private static partial Regex GetAssetReferenceRegex();
    
    [GeneratedRegex(@"{fileID:\s(?<fileId>[0-9A-Za-z]+)}", RegexOptions.Compiled)]
    private static partial Regex GetAssetSubReferenceRegex();
}
