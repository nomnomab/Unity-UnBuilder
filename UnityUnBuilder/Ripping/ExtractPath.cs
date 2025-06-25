using System.Reflection;

namespace Nomnom;

public record ExtractPath(string folderPath) {
    /// <summary>
    /// Constructs a <see cref="Nomnom.ExtractPath"/> from a folder path.
    /// </summary>
    /// <param name="folderPath">The path to the extraction folder.</param>
    public static ExtractPath FromFolder(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            // throw new DirectoryNotFoundException(folderPath);
            Directory.CreateDirectory(folderPath);
        }
        
        return new ExtractPath(folderPath);
    }
    
    /// <summary>
    /// Constructs a <see cref="Nomnom.ExtractPath"/> from a folder
    /// relative to the output directory.
    /// </summary>
    /// <param name="folderName">The name of the output folder in the build directory.</param>
    public static ExtractPath FromOutputFolder(string folderName) {
        var exePath = Assembly.GetEntryAssembly()?.Location;
        if (!File.Exists(exePath)) {
            throw new FileNotFoundException(exePath);
        }
        
        var outputPath = Path.GetFullPath(
            Path.Combine(
                exePath,
                "..",
                "output",
                folderName
            )
        );
        
        return FromFolder(outputPath);
    }
};
