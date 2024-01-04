using DTLib.Dtsod;
using DTLib.Filesystem;

namespace launcher_client;

public class LauncherConfig
{
    public int GameMemory = 3000;
    public int GameWindowHeight = 500;
    public int GameWindowWidth = 900;
    public string JavaPath = "java\\bin";
    public string ServerAddress = "127.0.0.1";
    public int ServerPort = 25000;
    public string Username = "";
    public string UUID = "";

    public string ConfigPath;
    
    
    public LauncherConfig(){}
    
    public LauncherConfig(DtsodV23 dtsod, string configPath)
    {
        GameMemory = dtsod["gameMemory"];
        GameWindowHeight = dtsod["gameWindowHeight"];
        GameWindowWidth = dtsod["gameWindowWidth"];
        JavaPath = dtsod["javaPath"];
        ServerAddress = dtsod["serverAddress"];
        ServerPort = dtsod["serverPort"];
        Username = dtsod["username"];
        UUID = dtsod["uuid"];

        ConfigPath = configPath;
    }

    public LauncherConfig(string configPath) :
        this(new DtsodV23(File.ReadAllText(configPath)), configPath)
    { }

    public DtsodV23 ToDtsod()
    {
        return new()
        {
            { "gameMemory", GameMemory },
            { "gameWindowHeight", GameWindowHeight },
            { "gameWindowWidth", GameWindowWidth },
            { "javaPath", JavaPath },
            { "serverAddress", ServerAddress },
            { "serverPort", ServerPort },
            { "username", Username },
            { "uuid", UUID }
        };
    }

    public void Save()
    {
        File.WriteAllText(ConfigPath, ToDtsod().ToString());
    }
    
    public static LauncherConfig CreateDefault(string configPath)
    {
        var c = new LauncherConfig
        {
            ConfigPath = configPath
        };
        c.Save();
        return c;
    }
}