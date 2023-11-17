using DTLib.Logging;

namespace Launcher.Client;

public class BufferedLogger : ILogger
{
    public bool DebugLogEnabled { get; set; } =
#if DEBUG
        true;
#else 
        false;
#endif
    public bool InfoLogEnabled { get; set; } = true;
    public bool WarnLogEnabled { get; set; } = true;
    public bool ErrorLogEnabled { get; set; } = true;

    public ILogFormat Format { get; set; } = new DefaultLogFormat();
    
    private readonly StringBuilder _buffer = new();
    public string Buffer 
    {
        get { lock (_buffer) return _buffer.ToString(); }
    }

    public event Action<string> MessageSent;
    

    public void Log(string context, LogSeverity severity, object message, ILogFormat format)
    {
        string msgConnected = Format.CreateMessage(context, severity, message);
        MessageSent?.Invoke(msgConnected);
        lock (_buffer) _buffer.Append(msgConnected);
    }

    public void Dispose()
    {
    }
}