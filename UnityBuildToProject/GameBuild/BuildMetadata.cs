using System.Diagnostics;

namespace Nomnom;

public record BuildPath(string exePath) {
    /// <summary>
    /// Constructs a <see cref="Nomnom.BuildPath"/> from an executable path.
    /// </summary>
    /// <param name="exePath">The path to the executable.</param>
    public static BuildPath FromExe(string exePath) {
        if (Path.GetExtension(exePath) is not ".exe") {
            throw new Exception("Nomnom.BuildPath requires a path to the game's executable.");
        }
        
        var parent = Directory.GetParent(exePath);
        if (parent == null) {
            throw new DirectoryNotFoundException(exePath);
        }
        
        return new BuildPath(exePath);
    }
    
    /// <summary>
    /// Returns if the executable exists.
    /// </summary>
    public bool Exists() {
        return File.Exists(exePath);
    }
    
    /// <summary>
    /// Returns the file info for the executable.
    /// </summary>
    public FileInfo GetFile() {
        return new FileInfo(exePath);
    }
    
    /// <summary>
    /// Returns the file info for the executable.
    /// </summary>
    public FileVersionInfo GetFileVersionInfo() {
        return FileVersionInfo.GetVersionInfo(exePath);
    }
    
    /// <summary>
    /// Returns the parent folder directory info, if it exists.
    /// </summary>
    public DirectoryInfo? GetParentFolder() {
        return Directory.GetParent(exePath);
    }
    
    /// <summary>
    /// Returns the path to the build's data folder.
    /// </summary>
    public DirectoryInfo? GetDataFolder() {
        var parent = GetParentFolder();
        if (parent == null) {
            return null;
        }
        
        // grab the file name since the [root]/*_Data folder
        var file = GetFile();
        var name = file.Name;
        var dataPath = $"{name}_Data";
        
        var path = Path.Join(
            parent.FullName,
            dataPath
        );
        
        return new DirectoryInfo(path);
    }
    
    /// <summary>
    /// Returns the path to the build's [root]/*_Data/Managed folder.
    /// </summary>
    public DirectoryInfo? GetManagedFolder() {
        var dataFolder = GetDataFolder();
        if (dataFolder == null) {
            return null;
        }
        
        var path = Path.Join(
            dataFolder.FullName,
            "Managed"
        );
        
        return new DirectoryInfo(path);
    }
};

public record BuildMetadata {
    public required BuildPath Path;
    public required string UnityVersion;
    
    /// <summary>
    /// Returns the metadata found from the game's build.
    /// </summary>
    /// <param name="buildPath">The game's path.</param>
    public static BuildMetadata Parse(BuildPath buildPath) {
        if (!buildPath.Exists()) {
            throw new FileNotFoundException(buildPath.exePath);
        }
        
        // get file info to read from
        var file = buildPath.GetFileVersionInfo();
        
        // get unity version
        var unityVersion = file.FileVersion;
        if (unityVersion != null) {
            var splitVersion = unityVersion.Split('.');
            if (splitVersion.Length > 2) {
                unityVersion = string.Join('.', splitVersion[..3]);
            }
        } else {
            throw new Exception($"Failed to load unity version from \"{buildPath.exePath}\".");
        }
        
        return new BuildMetadata {
            Path         = buildPath,
            UnityVersion = unityVersion
        };
    }
}
