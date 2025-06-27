namespace Nomnom;

public static class Profiling {
    public static ProfileStage? CurrentStage { get; private set; }
    public static TextWriter? CurrentWriter { get; private set; }
    
    public static ProfileDuration TotalDuration { get; private set; } = new();
    
    public static void Reset() {
        TotalDuration = new ProfileDuration();
    }
    
    public static ProfileStage Begin(string name, string message) {
        TotalDuration.New();
        
        var idx       = TotalDuration.Timestamps.Count;
        var stage     = new ProfileStage($"{idx}_{name}", message);
        CurrentStage  = stage;
        
        var logPath   = stage.LogPath;
        var dir       = Path.GetDirectoryName(logPath)!;
        Directory.CreateDirectory(dir);
        CurrentWriter = new StreamWriter(logPath);
        
        LogFile.Header(message);
        
        return stage;
    }
    
    public static async Task End() {
        await Task.Delay(1000);
        
        if (CurrentStage != null && CurrentWriter != null) {
            TotalDuration.Record(CurrentStage.Message);
            CurrentStage.End();
            
            CurrentWriter.WriteLine();
            CurrentWriter.WriteLine();
            CurrentWriter.WriteLine("---------------------");
            
            var duration = CurrentStage.Duration;
            CurrentWriter.WriteLine($"Duration: {duration}");
            
            await CurrentWriter.FlushAsync();
            await CurrentWriter.DisposeAsync();
        } else {
            TotalDuration.Record("no_message");
        }
        
        CurrentStage  = null;
        CurrentWriter = null;
    }
}

public record ProfileStage {
    private readonly DateTime _startTime;
    private DateTime _endTime;
    
    private readonly string _name;
    private readonly string _message;
    
    public string LogPath => Path.Combine(
        Paths.ToolLogsFolder,
        "stages",
        $"stage_{_name}.log"
    );
    
    public TimeSpan Duration => _endTime - _startTime;
    public string Message => _message;
    
    public ProfileStage(string name, string message) {
        _startTime = DateTime.Now;
        _name      = name;
        _message   = message;
    }
    
    public void End() {
        _endTime = DateTime.Now;
    }
}
