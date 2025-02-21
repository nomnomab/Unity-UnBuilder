namespace Nomnom;

public record UnityVersionsPath(string folderPath) {
    /// <summary>
    /// Constructs a <see cref="Nomnom.UnityVersionsPath"/> from a folder path.
    /// </summary>
    /// <param name="folderPath">The path to the folder that contains all the unity installations.</param>
    public static UnityVersionsPath FromFolder(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            throw new DirectoryNotFoundException(folderPath);
        }
        
        return new UnityVersionsPath(folderPath);
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
};
