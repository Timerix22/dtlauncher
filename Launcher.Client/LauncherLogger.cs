using DTLib.Logging;

namespace Launcher.Client;

public class LauncherLogger : CompositeLogger
{
    public static readonly IOPath LogfileDir = "launcher-logs";
    public readonly string LogfileName;
    public event Action<string> MessageSent;
    public string Buffer => _bufferedLogger.Buffer;

        
    FileLogger _fileLogger = new(LogfileDir,"launcher-client");
    ConsoleLogger _consoleLogger = new();
    BufferedLogger _bufferedLogger = new BufferedLogger();
    
    public LauncherLogger()
    {
        _loggers = new ILogger[] { _fileLogger, _consoleLogger, _bufferedLogger };
        LogfileName = _fileLogger.LogfileName.Str;
        _bufferedLogger.MessageSent += s => MessageSent?.Invoke(s);
    }
}