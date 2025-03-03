using CommandLine;

namespace Nomnom;

public class ProgramArgs {
    [Option('p', "game_path", Required = true, HelpText = "The path to the Unity game's executable (.exe).")]
    public required string GameExecutablePath { get; set; }
    
    [Option('s', "skip_ar", HelpText = "Skips the AssetRipper stage. Useful if you already exported the project.")]
    public bool SkipAssetRipper { get; set; }
    
    [Option('s', "skip_pack", HelpText = "Skips the package fetching stage. Useful if you already got the package list.")]
    public bool SkipPackageFetching { get; set; }
}

public class ProgramArgsParser {
    public static ParserResult<ProgramArgs>? Parse(string[] args) {
        return Parser.Default.ParseArguments<ProgramArgs>(args);
    }
}
