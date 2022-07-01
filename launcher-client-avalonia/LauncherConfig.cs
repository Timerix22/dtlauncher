namespace Launcher.Client.Avalonia;

public class LauncherConfig
{

    public record struct Server(IPEndPoint EndPoint, string Domain)
    {
        public Server(string domain, int port) : this
            (new IPEndPoint(Dns.GetHostAddresses(domain)[0], port), domain)
        { }
        public Server(IPAddress address, int port) : this
            (new IPEndPoint(address, port), "")
        { }
    }
    
    public const int Version=1;
    public Server[] ServerAddresses;

    const string configFile = "launcher.dtsod";
    public LauncherConfig()
    {
        // читает дефолтный конфиг из ресурсов
        DtsodV23 updatedConfig;
        DtsodV23 updatedDefault = new(EmbeddedResources.ReadText("Launcher.Client.Avalonia.Resources.launcher.dtsod"));
        // проверка и обновление конфига
        if (File.Exists(configFile))
        {
            DtsodV23 oldConfig = new(File.ReadAllText(configFile));
            updatedConfig = DtsodConverter.UpdateByDefault(oldConfig, updatedDefault);
        }
        else updatedConfig = updatedDefault;

        // парсит парсит полученный дтсод в LauncherConfig
        List<object> serversD = updatedConfig["server"];
        ServerAddresses = new Server[serversD.Count];
        ushort i = 0;
        foreach (DtsodV23 serverD in serversD)
        {
            int port = serverD["port"];
            // server must have <domain> or <ip> property
            ServerAddresses[i++] = serverD.TryGetValue("domain", out dynamic dom)
                ? new Server(dom, port)
                : new Server(IPAddress.Parse(serverD["ip"]), port);
        }
        
        WriteToFile();
    }

    // записывает обновлённый конфиг в файл
    public void WriteToFile()
    {
        StringBuilder b = new();
        b.Append("version: ").Append(Version).Append(";\n");
        foreach (var server in ServerAddresses)
        {
            b.Append("$server: {\n\t");
            if (server.Domain == "")
                b.Append("ip: \"").Append(server.EndPoint.Address);
            else b.Append("domain: \"").Append(server.Domain);
            b.Append("\";\n\tport: ")
                .Append(server.EndPoint.Port)
                .Append(";\n};\n");
        }
    }
}