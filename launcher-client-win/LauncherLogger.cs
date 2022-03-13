using System.Globalization;
using DTLib.Loggers;

namespace launcher_client_win;

public class LauncherLogger : BaseLogger
{
    public string Buffer="";

    public LauncherLogger() : base("launcher-logs", "launcher-client-win")
    { }
    
    public event Action<string> MessageSent;
    
    public override void Log(params string[] msg)
    {
        lock (Logfile) if (!IsEnabled) return;
        
        StringBuilder strB = new();
        strB.Append('[')
            .Append(DateTime.Now.ToString(CultureInfo.InvariantCulture))
            .Append("]: ");
        if (msg.Length == 1) strB.Append(msg[0]);
        else for (ushort i = 0; i < msg.Length; i++)
                strB.Append(msg[++i]);
        strB.Append('\n');
        string _buffer = strB.ToString();
        
        lock(Buffer) Buffer += _buffer;
        
        MessageSent?.Invoke(_buffer);
        
        if (WriteToFile)
            lock(Logfile) File.AppendAllText(Logfile, _buffer);
    }
}