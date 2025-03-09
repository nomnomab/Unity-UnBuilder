using System.Text;
using AssetRipper.SourceGenerated.Enums;
using Spectre.Console;

namespace Nomnom;

public static class FixTextures {
    public static void FixFormat(string projectPath, string[]? extensions, Func<string, TextureFormat?> predicate) {
        extensions ??= [
            ".png",
            ".jpg",
            ".jpeg"
        ];
        
        var assets = Path.Combine(projectPath, "Assets");
        var textures = Directory.GetFiles(assets, "*.*", System.IO.SearchOption.AllDirectories)
            .Where(x => extensions.Contains(Path.GetExtension(x)));
        
        foreach (var texture in textures) {
            var format = predicate(texture);
            if (format == null) continue;
            
            var file = texture + ".meta";
            if (!File.Exists(file)) {
                throw new FileNotFoundException(file);
            }
            
            // find 'platformSettings'
            // then the first 'textureFormat'
            var foundPlatformSettings = false;
            var fixedTexture          = false;
            var sb                    = new StringBuilder();
            using (var reader = new StreamReader(file)) {
                while (reader.Peek() >= 0) {
                    // read lines
                    var line = reader.ReadLine();
                    if (line == null) continue;
                    
                    if (foundPlatformSettings && line.Trim().StartsWith("textureFormat:")) {
                        var prefix = line.TrimStart();
                        line = $"{prefix}textureFormat: {(int)format}";
                        fixedTexture = true;
                        sb.AppendLine(line);
                        break;
                    }
                    
                    if (line.Trim().StartsWith("platformSettings:")) {
                        foundPlatformSettings = true;
                        sb.AppendLine(line);
                        continue;
                    }
                    
                    sb.AppendLine(line);
                }
                
                // flush lines
                while (reader.Peek() >= 0) {
                    // read lines
                    var line = reader.ReadLine();
                    if (line == null) continue;
                    
                    sb.AppendLine(line);
                }
            }
            
            if (fixedTexture) {
                AnsiConsole.WriteLine($"Converted {file} to {format}");
                File.WriteAllText(file, sb.ToString());
            } else {
                if (!foundPlatformSettings) {
                    AnsiConsole.WriteLine("No \"platformSettings:\" found");
                } else {
                    AnsiConsole.WriteLine("No \"textureFormat:\" found under platformSettings");
                }
                
                throw new Exception($"Failed to convert {file} to {format}!");
            }
        }
    }
}
