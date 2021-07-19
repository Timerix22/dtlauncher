using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static updater.Program;

namespace updater
{
    // 
    // весь униврсальный неткод тут
    // большинство методов предназначены для работы с TCP сокетами (Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
    //
    public static class NetWork
    {
        /*
        // получение информации (сокет должен быть в режиме Listen)
        public static byte[] GetData(this Socket socket)
        {
            List<byte> output = new List<byte>();
            byte[] data = new byte[256];
            do
            {
                int amount = socket.Receive(data, data.Length, 0);
                for (int i = 0; i < amount; i++)
                {
                    output.Add(data[i]);
                }
            }
            while (socket.Available > 0);
            return output.ToArray();
        }

        // отправка запроса и получение ответа на него (сокет должен быть в режиме Listen)
        public static byte[] Request(this Socket socket, string request)
        {
            socket.Send(request.ToBytes());
            return GetData(socket);
        }
        public static byte[] Request(this Socket socket, byte[] request)
        {
            socket.Send(request);
            return GetData(socket);
        }
        */

        // скачивание файла с фтп сервера
        public static void FtpDownload(string address, string login, string password, string outfile)
        {
            try
            {
                // debug
                Log("y", "file on server: <", "c", address, "y", ">\nfile on client: <", "c", outfile, "y", ">\n" );
                // создание запроса
                // "ftp://m1net.keenetic.pro:20000/" + @infile
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(address);
                request.Credentials = new NetworkCredential(login, password);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                // получение ответа на запрос
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                FileStream fs = new FileStream(@Directory.GetCurrentDirectory() + '\\' + @outfile, FileMode.Create);
                byte[] buffer = new byte[64];
                int size = 0;

                while ((size = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    fs.Write(buffer, 0, size);
                fs.Close();
                response.Close();
            }
            catch (WebException e) { throw new Exception("ftp error:\n" + ((FtpWebResponse)e.Response).StatusDescription + '\n'); }
        }

        // запрашивает список серверов с главного сервера
        /*public static ServerObject[] RequestServersList(Socket centralServer)
        {
            List<ServerObject> servers = new List<ServerObject>();
            string[] lines = Request(centralServer, "a356a4257dbf9d87c77cf87c3c694b30160b6ddfd3db82e7f62320207109e352").ToStr().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string[] properties = lines[i].Split(',');
                servers.Add(new ServerObject(properties[0], properties[1], properties[2]));
            }
            return servers.ToArray();
        }*/

        // пингует айпи с помощью встроенной в винду проги, возвращает задержку
        public static string PingIP(string address)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/c @echo off & chcp 65001 >nul & ping -n 5 " + address;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            var outStream = proc.StandardOutput;
            var rezult = outStream.ReadToEnd();
            rezult = rezult.Remove(0, rezult.LastIndexOf('=') + 2);
            return rezult.Remove(rezult.Length - 4);
        }

        // пингует сервер (сервер должен уметь принимать такой запрос от клиента), возвращает true если сервер правильно ответил
        /*public static bool Ping(this Socket socket)
        {
            if (Request(socket, "ab53bf045004875fb17086f7f992b0514fb96c038f336e0bfc21609b20303f07").ToStr() == "91b5c0383b75fb1708f00486f7f9b96c038ab3bfc21059b20176f603692b05e0") return true;
            else return false;
        } */

        // скачивает файл с помощью FSP протокола
        public static void FSP_Download(this Socket mainSocket, FSP_FileObject file)
        {
            Log(new string[] { "c", $"remote socket accepted download request: {file.ClientFilePath}\n" });
            mainSocket.Send("requesting file download".ToBytes());
            string answ = mainSocket.GetPackageClear(64).ToStr();
            if (answ != "download request accepted")
                throw new Exception($"FSP_Download() error: a download socket recieved an incorrect message: {answ}\n");

            mainSocket.SendPackage(256, file.ServerFilePath.ToBytes());
            file.Size = Convert.ToUInt32(mainSocket.GetPackageClear(64).ToStr());
            file.Hash = mainSocket.GetPackageClear(64);
            mainSocket.SendPackage(64, "ready".ToBytes());
            file.Stream = File.Open(file.ClientFilePath, FileMode.Create, FileAccess.Write);
            int packagesCount = 0;
            byte[] buffer = new byte[5120];
            var hashstr = file.Hash.HashToString();
            int fullPackagesCount = SimpleConverter.Truncate(file.Size / buffer.Length);
            // рассчёт скорости
            int seconds = 0;
            var speedCounter = new Timer(true, 1000, () =>
            {
                seconds++;
                Log("c", $"speed= {packagesCount * buffer.Length / (seconds * 1000)} kb/s\n");
            });
            // получение файла
            for (; packagesCount < fullPackagesCount; packagesCount++)
            {
                buffer = mainSocket.GetPackageRaw(buffer.Length);
                file.Stream.Write(buffer, 0, buffer.Length);
                file.Stream.Flush();
            }
            Log(new string[] { "y", "   full packages recieved\n" });
            speedCounter.Stop();
            // получение остатка
            mainSocket.SendPackage(64, "remain request".ToBytes());
            buffer = mainSocket.GetPackageRaw((file.Size - fullPackagesCount * 5120).ToInt());
            file.Stream.Write(buffer, 0, buffer.Length);
            file.Stream.Flush();
            file.Stream.Close();
            Log(new string[] { "g", $"   file {file.ClientFilePath} ({packagesCount * 5120 + buffer.Length} of {file.Size} bytes) downloaded.\n" });
        }

        // отдаёт файл с помощью FSP протокола
        public static void FSP_Upload(this Socket mainSocket)
        {
            mainSocket.SendPackage(64, "download request accepted".ToBytes());
            var file = new FSP_FileObject
            {
                ServerFilePath = mainSocket.GetPackageClear(256).ToStr()
            };
            file.Size = new FileInfo(file.ServerFilePath).Length;
            file.Hash = new Hasher().HashFile(file.ServerFilePath);
            mainSocket.SendPackage(64, file.Size.ToString().ToBytes());
            mainSocket.SendPackage(64, file.Hash);
            if (mainSocket.GetPackageClear(64).ToStr() != "ready") throw new Exception("user socket isn't ready");
            Log(new string[] { "c", $"local socket accepted file download request: {file.ServerFilePath}\n" });
            file.Stream = new FileStream(file.ServerFilePath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[5120];
            var hashstr = file.Hash.HashToString();
            int packagesCount = 0;
            int seconds = 0;
            // рассчёт скорости
            var speedCounter = new Timer(true, 1000, () =>
            {
                seconds++;
                Log("c", $"speed= {packagesCount * buffer.Length / (seconds * 1000)} kb/s\n");
            });
            // отправка файла
            int fullPackagesCount = SimpleConverter.Truncate(file.Size / buffer.Length);
            for (; packagesCount < fullPackagesCount; packagesCount++)
            {
                file.Stream.Read(buffer, 0, buffer.Length);
                mainSocket.SendPackage(buffer.Length, buffer);
            }
            Log(new string[] { "y", "   full packages send\n" });
            speedCounter.Stop();
            // досылка остатка
            if (mainSocket.GetPackageClear(64).ToStr() != "remain request")
                throw new Exception("FSP_Upload() error: didn't get remain request");
            buffer = new byte[Convert.ToInt32(file.Size - file.Stream.Position)];
            file.Stream.Read(buffer, 0, buffer.Length);
            mainSocket.SendPackage(buffer.Length, buffer);
            file.Stream.Close();
            Log(new string[] { "g", $"   file {file.ServerFilePath} ({packagesCount * 5120 + buffer.Length} of {file.Size} bytes) uploaded.\n" });
        }

        // ждёт пакет заданного размера с заданным началом и концом
        // убирает пустые байты в конце пакета
        public static byte[] GetPackageClear(this Socket socket, int packageSize)
        {
            byte[] data = socket.GetPackageRaw(packageSize);
            bool clear = false;
            int toClean = packageSize;
            for (int i = packageSize - 1; !clear; i--)
            {
                if (data[i] == 00) toClean--;
                else clear = true;
            }
            if (toClean != packageSize) data = data.RemoveRange(toClean);
            return data;
        }
        //не убирает пустые байты в конце пакета
        public static byte[] GetPackageRaw(this Socket socket, int packageSize)
        {
            var startsWith = ("<" + packageSize.ToString() + ">").ToBytes();
            packageSize += startsWith.Length;
            byte[] data = new byte[packageSize];
            // цикл выполняется пока не пройдёт 1000 мс
            for (short s = 0; s < 1000; s += 10)
            {
                if (socket.Available >= packageSize)
                {
                    socket.Receive(data, packageSize, 0);
                    if (data.StartsWith(startsWith)) return data.RemoveRange(0, startsWith.Length);
                    else throw new Exception($"GetPackage() error: has got incorrect package\n");
                }
                else Thread.Sleep(10);
            }
            throw new Exception($"GetPackage() error: timeout\n");
        }

        // отправляет пакет заданного размера, добавля в конец нули если длина data меньше чем packageSize
        public static void SendPackage(this Socket socket, int packageSize, byte[] data)
        {
            if (data.Length > packageSize) throw new Exception("SendPackage() error: data.Length > packageSize\n");
            var startsWith = ("<" + packageSize.ToString() + ">").ToBytes();
            packageSize += startsWith.Length;
            var list = new List<byte>();
            list.AddRange(startsWith);
            list.AddRange(data);
            for (int i = startsWith.Length + data.Length; i < packageSize; i++) list.Add(0);
            socket.Send(list.ToArray());
        }


        // хранит свойства файла, передаваемого с помощью моего протокола
        public class FSP_FileObject
        {
            public string ServerFilePath;
            public string ClientFilePath;
            public long Size;
            public byte[] Hash;
            public Stream Stream;

            public FSP_FileObject() { }
        }

        // хранит свойства сервера, полученные с помощью RequestServersList()
        public class ServerObject
        {
            public string Address;
            public string Name;
            public string Speed;

            public ServerObject(string address, string name, string speed)
            {
                Address = address;
                Name = name;
                Speed = speed;
            }

            public ServerObject() { }
        }
    }
}
