using CommandLine;

namespace Nomnom;

public class ProgramArgs {
    [Option('p', "game_path", Required = true, HelpText = "The path to the Unity game's executable (.exe).")]
    public required string GameExecutablePath { get; set; }
}

public class ProgramArgsParser {
    public static ParserResult<ProgramArgs>? Parse(string[] args) {
        return Parser.Default.ParseArguments<ProgramArgs>(args);
    }
}
