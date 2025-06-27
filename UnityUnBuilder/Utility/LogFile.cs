using System.Text;
using System.Text.RegularExpressions;

namespace Nomnom;

public static class LogFile {
    private static LogFileWriter? _writer;
    
    public static void Create() {
        _writer = new LogFileWriter();
        Console.SetOut(_writer);
    }
    
    public static void Close() {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }
    
    public static void Header(string message) {
        Space(2);
        Console.WriteLine(message);
        
        var length = message.Length + 10;
        var spacer = new string('-', length);
        Console.WriteLine(spacer);
        
        Space();
    }
    
    public static void Space(int count = 1) {
        for (int i = 0; i < count; i++) {
            Console.WriteLine();
        }
    }
}

partial class LogFileWriter : TextWriter {
    private const string NAME = "tool.log";
    private static string Path => System.IO.Path.Combine(Paths.ToolLogsFolder, NAME);
    
    // private static readonly Regex _bracketRegex = BracketRegex();
    
    private readonly TextWriter _out;
    private readonly TextWriter _log;
    
    public LogFileWriter() {
        _out = Console.Out;
        
        File.Delete(Path);
        _log = new StreamWriter(Path);
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        
        if (disposing) {
            Console.SetOut(_out);
            _log.Dispose();
        }
    }
    
    public override void Write(char value) {
        _out.Write(value);
        _log.Write(value);
        
        if (Profiling.CurrentWriter is {} stageWriter) {
            stageWriter.Write(value);
        }
    }
    
    public override void Write(string? value) {
        // var text = _bracketRegex.Replace(value ?? string.Empty, string.Empty);
        
        _out.Write(value);
        _log.Write(value);
        
        if (Profiling.CurrentWriter is {} stageWriter) {
            stageWriter.Write(value);
        }
    }

    public override Encoding Encoding => Encoding.UTF8;

    // [GeneratedRegex(@"\[.*?\]", RegexOptions.Compiled)]
    // private static partial Regex BracketRegex();
}
