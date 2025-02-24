using System.Text.RegularExpressions;
using Spectre.Console;

namespace Nomnom;

public sealed partial class GuidMapping {
    private readonly static Regex GuidPattern = GetGuidRegex();
    private readonly static Regex FileIdPattern = GetFileIdRegex();
    private readonly static Regex FileIdReferencePattern = GetFileIdReferenceRegex();
    
    private readonly ExtractData _extractData;
    
    public GuidMapping(ExtractData extractData) {
        _extractData = extractData;
    }
    
    public async Task ExtractGuids() {
        var tempProjectPath = _extractData.GetTempProjectPath();
        
        // get all meta files in the entire tree
        // this will take a bit
        var metaFiles = Directory.EnumerateFiles(tempProjectPath, "*.*", SearchOption.AllDirectories)
            // find meta and asset files
            .Where(x => {
                if (!Path.HasExtension(x)) {
                    return false;
                }
                var end = Path.GetExtension(x).ToLower();
                return end == ".meta" || end == ".asset";
            });
        
        foreach (var file in metaFiles) {
            // get relative file span to project
            var relativeFileSpan = file.AsSpan()[tempProjectPath.Length..];
            var tree = new Tree(relativeFileSpan.ToString());
            
            using var reader = new StreamReader(file);
            while (reader.Peek() >= 0) {
                // read lines
                var line = reader.ReadLine();
                if (line == null) continue;
                
                // parse guid
                var guids = GuidPattern.Matches(line);
                foreach (Match guid in guids) {
                    tree.AddNode(guid.Value);
                }
                
                // parse fileid
                var fileIds = FileIdPattern.Matches(line);
                foreach (Match fileId in fileIds) {
                    tree.AddNode(fileId.Value);
                }
            }
            
            AnsiConsole.Write(tree);
        }
    }

    [GeneratedRegex(@"guid:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled)]
    private static partial Regex GetGuidRegex();
    
    [GeneratedRegex(@"fileID:\s(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled)]
    private static partial Regex GetFileIdRegex();
    
    [GeneratedRegex(@"--- !u!\w+\s&(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled)]
    private static partial Regex GetFileIdReferenceRegex();
}
