using AssetRipper.SourceGenerated;

namespace Nomnom;

/// <summary>
/// Simply stores a guid.
/// </summary>
public record MetaFile {
    public required string FilePath;
    public required UnityGuid Guid;
}

/// <summary>
/// Can store many guid references, as well as file id references.
/// </summary>
public record AssetFile {
    /// <summary>
    /// The absolute file path to the asset.
    /// </summary>
    public required string FilePath;
    public required HashSet<UnityObjectDefinition> Objects;
    
    public static AssetFile Default => new() {
        FilePath = "",
        Objects  = [],
    };
}

public record ShaderFile {
    /// <summary>
    /// The absolute file path to the asset.
    /// </summary>
    public required string FilePath;
    
    /// <summary>
    /// The lookup path name. Is basically a path.
    /// </summary>
    public required string Name;
}

/// <summary>
/// An id to an asset.
/// </summary>
public record UnityGuid(string Value) {
    public override string ToString() {
        return Value;
    }
}

/// <summary>
/// This part defines the ID for the object itself, which is used to reference objects between each other.
/// It’s called File ID because it represents the ID of the object in a specific file.
/// </summary>
public record UnityFileId(string Value) {
    public override string ToString() {
        return Value;
    }
}

/// <summary>
/// Type is used to determine whether the file should be loaded from the Assets folder or the Library folder.
/// Note that it only supports the following values, starting at 2 (given that 0 and 1 are deprecated).
/// <br/><br/>
/// Another factor to highlight regarding script serialization is that the YAML Type is the same for every script; just MonoBehaviour.
/// The actual script is referenced in the “m_Script” property, using the GUID of the script’s meta file.
/// </summary>
public record UnityFileType(int Value) {
    /// <summary>
    /// Assets that can be loaded directly from the Assets folder by the Editor,
    /// like Materials and .asset files.
    /// </summary>
    /// <returns></returns>
    public bool IsType2() => Value == 2;
    
    /// <summary>
    /// Assets that have been processed and written in the Library folder, and
    /// loaded from there by the Editor, like Prefabs, textures, and 3D models.
    /// </summary>
    /// <returns></returns>
    public bool IsType3() => Value == 3;
    
    public override string ToString() {
        if (IsType2()) return "Type2";
        if (IsType3()) return "Type3";
        return Value.ToString();
    }
}

/// <summary>
/// A reference to another asset.
/// <br/><br/>
/// In the format of: <code>{fileID: [fileId], guid: [guid], type: [type]}</code>
/// </summary>
public record UnityAssetReference {
    public required UnityFileId FileId;
    public required UnityGuid Guid;
    public required UnityFileType Type;

    public override string ToString() {
        return $"{FileId}:{Guid}:{Type}";
    }
}

/// <summary>
/// An object stored in an asset file.
/// <br/><br/>
/// Starts with a format such as: <code>--- !u![classId] &amp;[fileId]</code>
/// </summary>
public record UnityObjectDefinition {
    public required ClassIDType ClassId;
    public required UnityFileId FileId;
    
    public required List<UnityAssetReference> AssetReferences;
    public required List<UnityFileId> NestedReferences;

    public override string ToString() {
        return $"{ClassId}:{FileId}";
    }
}
