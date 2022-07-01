using DTLib.Loggers;

namespace Launcher.Client.WPF;

public class LauncherLogger : ConsoleLogger
{
    public const string LogfileDir = "launcher-logs";
    public LauncherLogger() : base(LogfileDir,"launcher-client-wpf")
    { }
    
    private readonly StringBuilder _buffer = new();
    public string Buffer  
    {
        get { lock (_buffer) return _buffer.ToString(); }
    }

    public event Action<string> MessageSent;
    
    public override void Log(params string[] msg)
    {
        base.Log(msg);
        StringBuilder strb = new();
        if (msg.Length == 1) strb.Append(msg[0]);
        else for (int i = 1; i < msg.Length; i += 2)
            strb.Append(msg[i]);
        strb.Append('\n');
        string msgConnected = strb.ToString();
        MessageSent?.Invoke(msgConnected);
        lock (_buffer) _buffer.Append(msgConnected);
    }
}