namespace Nomnom;

public record UnityInstallsPath(string folderPath) {
    /// <summary>
    /// Constructs a <see cref="Nomnom.UnityInstallsPath"/> from a folder path.
    /// </summary>
    /// <param name="folderPath">The path to the folder that contains all the unity installations.</param>
    public static UnityInstallsPath FromFolder(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            throw new DirectoryNotFoundException(folderPath);
        }
        
        return new UnityInstallsPath(folderPath);
    }
};

public record UnityPath(string folderPath) {
    /// <summary>
    /// Constructs a <see cref="Nomnom.UnityPath"/> from a folder path.
    /// </summary>
    /// <param name="folderPath">The path to the unity installation.</param>
    public static UnityPath FromFolder(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            throw new DirectoryNotFoundException(folderPath);
        }
        
        return new UnityPath(folderPath);
    }
    
    /// <summary>
    /// Constructs a <see cref="Nomnom.UnityPath"/> from a specific version.
    /// </summary>
    /// <param name="folderPath">The path to the unity installation.</param>
    public static UnityPath FromVersion(UnityInstallsPath versionsPath, string version) {
        var folder = Path.Combine(versionsPath.folderPath, version);
        if (!Directory.Exists(folder)) {
            // only skip 'f' versions, as others are required in the url
            var letterIndex = version.AsSpan().IndexOf("f");
            if (letterIndex != -1) {
                version = version[..letterIndex];
            }
            
            var downloadUrl = GetDownloadUrl(version);
            throw new DirectoryNotFoundException(
@$"Unity version ""{version}"" was not found. You may need to install it first!

You can install it from below:
{downloadUrl}"
            );
        }
        
        return new UnityPath(folder);
    }
    
    /// <summary>
    /// Returns the path to the editor's executable file.
    /// </summary>
    public string GetExePath() {
        // todo: support other platforms
        return Path.Combine(GetEditorPath(), "Unity.exe");
    }
    
    public string GetEditorPath() {
        return Path.Combine(folderPath, "Editor");
    }
    
    public string GetResourcesPath() {
        return Path.Combine(GetEditorPath(), "Data", "Resources");
    }
    
    public string GetPackageManagerPath() {
        return Path.Combine(GetResourcesPath(), "PackageManager");
    }
    
    public string GetBuiltInPackagesPath() {
        return Path.Combine(GetPackageManagerPath(), "BuiltInPackages");
    }
    
    public static string GetDownloadUrl(string version) {
        return $"https://unity.com/releases/editor/whats-new/{version}#installs";
    }
}
