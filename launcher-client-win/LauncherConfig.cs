namespace launcher_client_win;

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
        DtsodV23 updatedDtsod = new(ReadResource("launcher_client_win.Resources.launcher.dtsod"));
        // проверка и обновление конфига
        if (File.Exists(configFile))
        {
            DtsodV23 dtsod = new(File.ReadAllText(configFile));
            // заменяет дефолтные значения на пользовательские
            foreach (var p in dtsod)
            {
                if (updatedDtsod.TryGetValue(p.Key, out dynamic def))
                {
                    if (def.GetType() != p.Value.GetType())
                        throw new Exception(
                            "uncompatible config value type\n  " +
                            $"launcher.dtsod: {p.Key}:{p.Value} is {p.Value.GetType().Name}, " +
                            $"must be {def.GetType().Name}");
                    updatedDtsod[p.Key] = p.Value;
                }
            }
            // записывает обновлённый конфиг в файл
            WriteToFile();
        }

        // парсит парсит полученный дтсод в LauncherConfig
        List<object> serversD = updatedDtsod["server"];
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
    }

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