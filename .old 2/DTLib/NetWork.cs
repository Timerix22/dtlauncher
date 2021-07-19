using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DTLib
{
    // 
    // некоторые полезные методы для работы с TCP сокетами (Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
    //
    static public class NetWork
    {
        public delegate void LogDelegate(string[] msg);
        // можно присвоить методы этому делегату чтоб выводить логи
        static public LogDelegate Log;
        static void LogSimple(string color, string msg)
        {
            Log(new string[] { color, msg });
        }

        // правильно закрывает сокет
        static public void CloseSocket(this Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        // получение информации (сокет должен быть в режиме Listen)
        static public byte[] GetData(this Socket socket)
        {
            List<byte> output = new List<byte>();
            byte[] data = new byte[256];
            do
            {
                int amount = socket.Receive(data, data.Length, 0);
                for (int i = 0; i < amount; i++)
                    output.Add(data[i]);
            }
            while (socket.Available > 0);
            return output.ToArray();
        }

        // отправка запроса и получение ответа на него (сокет должен быть в режиме Listen)
        static public byte[] Request(this Socket socket, string request)
        {
            socket.Send(request.ToBytes());
            return GetData(socket);
        }

        static public byte[] Request(this Socket socket, byte[] request)
        {
            socket.Send(request);
            return GetData(socket);
        }

        /*static public MessageObject SplitMessage(byte[] recieved)
        {
            if (recieved.Length != 2284) throw new Exception("incorrect message length: " + recieved.Length);
            var msg = new MessageObject();
            msg.Number = recieved[2];
            for (byte i = 3; i < 9; i++)
            {
                msg.Number = msg.Number * 10 + recieved[i];
            }
            msg.Datetime = new DateTime(recieved[9] * 100 + recieved[10], recieved[11], recieved[12],
                recieved[13], recieved[14], recieved[15]);
            for (byte i = 16; i < 22; i++)
            {
                msg.Sender = msg.Sender * 10 + recieved[i];
            }
            for (byte i = 22; i < 28; i++)
            {
                msg.Channel = msg.Channel * 10 + recieved[i];
            }
            List<byte> text = new List<byte>();
            for (short i = 28; i < 2028; i++)
            {
                text.Add(recieved[i]);
            }
            msg.Text = text.ToStr();
            Log($"message <{msg.Number}> is had recieved");
            return msg;
        }*/

        static public void FtpDownload(string address, string login, string password, string outfile)
        {
            try
            {
                // debug
                Log(new string[] {
                    "y", "file on server: <", "c",address, "y",">\nfile on client: <",
                    "c",outfile,"y", ">\n"});
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
                {
                    fs.Write(buffer, 0, size);
                }
                fs.Close();
                response.Close();
            }
            catch (WebException e)
            {
                throw new Exception("ftp error:\n" + ((FtpWebResponse)e.Response).StatusDescription + '\n');
            }
        }

        static public ServerObject[] RequestServersList(Socket centralServer)
        {
            List<ServerObject> servers = new List<ServerObject>();
            string[] lines = Request(centralServer, "a356a4257dbf9d87c77cf87c3c694b30160b6ddfd3db82e7f62320207109e352").ToStr().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string[] properties = lines[i].Split(',');
                servers.Add(new ServerObject(properties[0], properties[1], properties[2]));
            }
            return servers.ToArray();
        }

        static public string PingIP(string address)
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

        static public bool Ping(this Socket socket)
        {
            var rec = Request(socket, "ab53bf045004875fb17086f7f992b0514fb96c038f336e0bfc21609b20303f07");
            if (rec.ToStr() == "91b5c0383b75fb1708f00486f7f9b96c038ab3bfc21059b20176f603692b05e0")
            {
                return true;
            }
            else return false;
        }

        static public void FSP_Download(this Socket mainSocket, FSP_FileObject file)
        {
            Log(new string[] { "c", $"remote socket accepted download request: {file.ClientFilePath}\n" });
            mainSocket.Send("requesting file download".ToBytes());
            string answ = mainSocket.GetPackageClear(64, "<", ">").ToStr();
            if (answ != "download request accepted")
                throw new Exception($"FSP_Download() error: a download socket recieved an incorrect message: {answ}\n");
            mainSocket.SendPackage(276, file.ServerFilePath.ToBytes(), "<filename>", "<filename>");
            file.Size = Convert.ToUInt32(mainSocket.GetPackageClear(64, "<filesize>", "<filesize>").ToStr());
            file.Hash = mainSocket.GetPackageClear(64, "<filehash>", "<filehash>");
            mainSocket.SendPackage(64, "ready".ToBytes(), "<", ">");
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
                LogSimple("c", $"speed= {packagesCount * buffer.Length / (seconds * 1000)} kb/s\n");
            });
            // получение файла
            for (; packagesCount < fullPackagesCount; packagesCount++)
            {
                string header = $"<{packagesCount}>";
                buffer = mainSocket.GetPackageRaw(buffer.Length + 2 + header.Length, header, "<>");
                file.Stream.Write(buffer, 0, buffer.Length);
                file.Stream.Flush();
            }
            Log(new string[] { "y", "   full packages recieved\n" });
            speedCounter.Stop();
            // получение остатка
            mainSocket.SendPackage(64, "remain request".ToBytes(), "<", ">");
            buffer = mainSocket.GetPackageRaw(Convert.ToInt32(file.Size - fullPackagesCount * 5120) + 16, "<remain>", "<remain>");
            file.Stream.Write(buffer, 0, buffer.Length);
            file.Stream.Flush();
            file.Stream.Close();
            Log(new string[] { "g", $"   file {file.ClientFilePath} ({packagesCount * 5120 + buffer.Length} of {file.Size} bytes) downloaded.\n" });
        }

        static public void FSP_Upload(this Socket mainSocket)
        {
            mainSocket.SendPackage(64, "download request accepted".ToBytes(), "<", ">");
            var file = new FSP_FileObject();
            file.ServerFilePath = mainSocket.GetPackageClear(276, "<filename>", "<filename>").ToStr();
            file.Size = new FileInfo(file.ServerFilePath).Length;
            file.Hash = new SecureHasher(256).HashFile(file.ServerFilePath);
            mainSocket.SendPackage(64, file.Size.ToString().ToBytes(), "<filesize>", "<filesize>");
            mainSocket.SendPackage(64, file.Hash, "<filehash>", "<filehash>");
            if (mainSocket.GetPackageClear(64, "<", ">").ToStr() != "ready")
                throw new Exception("user socket isn't ready");
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
                LogSimple("c", $"speed= {packagesCount * buffer.Length / (seconds * 1000)} kb/s\n");
            });
            // отправка файла
            int fullPackagesCount = SimpleConverter.Truncate(file.Size / buffer.Length);
            for (; packagesCount < fullPackagesCount; packagesCount++)
            {
                string header = $"<{packagesCount}>";
                file.Stream.Read(buffer, 0, buffer.Length);
                try { mainSocket.SendPackage(buffer.Length + 2 + header.Length, buffer, header, "<>"); }
                catch (Exception ex) { Log(new string[] { "r", "FSP_Upload() error: " + ex.Message + "\n" + ex.StackTrace + '\n' }); }
            }
            Log(new string[] { "y", "   full packages send\n" });
            speedCounter.Stop();
            // досылка остатка
            var req = mainSocket.GetPackageClear(64, "<", ">");
            if (req.ToStr() != "remain request")
            {
                throw new Exception("FSP_Upload() error: didn't get remain request");
            }
            buffer = new byte[Convert.ToInt32(file.Size - file.Stream.Position)];
            file.Stream.Read(buffer, 0, buffer.Length);
            mainSocket.SendPackage(buffer.Length + 16, buffer, "<remain>", "<remain>");
            file.Stream.Close();
            Log(new string[] { "g", $"   file {file.ServerFilePath} ({packagesCount * 5120 + buffer.Length} of {file.Size} bytes) uploaded.\n" });
        }

        // убирает пустые байты в конце пакета
        static public byte[] GetPackageClear(this Socket socket, int packageSize, string startsWith, string endsWith)
        {
            byte[] data = socket.GetPackageRaw(packageSize, startsWith, endsWith);
            bool clean = false;
            for (int i = 0; !clean; i++)
            {
                if (data[i] != 00)
                {
                    if (i != 0) data = data.Remove(0, i);
                    clean = true;
                }
                else clean = i == data.Length - 1;
            }
            return data;
        }
        //не убирает пустые байты в конце пакета
        static public byte[] GetPackageRaw(this Socket socket, int packageSize, string startsWith, string endsWith)
        {
            byte[] data = new byte[packageSize];
            //Log(new string[] { "y", $"GetPackage() packegesize=<","c",packageSize.ToString(),
            //    "y", "> startsWith=<", "c", startsWith, "y", "> endsWith=<", "c", endsWith, "y", ">\n" });
            for (short s = 0; s < 2000; s += 10)
            {
                if (socket.Available >= packageSize)
                {
                    socket.Receive(data, packageSize, 0);
                    if (data.StartsWith(startsWith) & data.EndsWith(endsWith))
                    {
                        data = data.Remove(0, startsWith.ToBytes().Length);
                        data = data.Remove(data.Length - endsWith.ToBytes().Length, endsWith.ToBytes().Length);
                        return data;
                    }
                    else throw new Exception($"GetPackage() error: has got incorrect package\n");
                }
                else Thread.Sleep(10);
            }
            throw new Exception($"GetPackage() error: timeout\n");
        }

        static public void SendPackage(this Socket socket, int length, byte[] data, string startsWith, string endsWith)
        {
            var list = new List<byte>();
            list.AddRange(startsWith.ToBytes());
            int i = startsWith.ToBytes().Length + endsWith.ToBytes().Length + data.Length;
            //Log(new string[] { "y", $"SendPackage() length=<","c",length.ToString(),"y", "> packegesize=<","c",i.ToString(),
            //    "y", "> data.Length=<","c",data.Length.ToString(), "y", "> startsWith=<", "c", startsWith, "y", "> endsWith=<", "c", endsWith, "y", ">\n" });
            if (i > length) throw new Exception("SendPackage() error: int length is too small\n");
            for (; i < length; i++)
                list.Add(0);
            list.AddRange(data);
            list.AddRange(endsWith.ToBytes());
            socket.Send(list.ToArray());
        }
    }

    public class FSP_FileObject
    {
        public string ServerFilePath;
        public string ClientFilePath;
        public long Size;
        public byte[] Hash;
        public Stream Stream;

        public FSP_FileObject(string serverFile, string clientFile)
        {
            ServerFilePath = serverFile;
            ClientFilePath = clientFile;
        }

        public FSP_FileObject() { }
    }

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
    }

    public class MessageObject
    {
        public uint Number;
        public DateTime Datetime;
        public int Sender;
        public int Channel;
        public string Text;

        public MessageObject() { }

        public MessageObject(byte[] recieved)
        {
            if (recieved.Length != 2284) throw new Exception("incorrect message length: " + recieved.Length);
            Number = recieved[2];
            for (byte i = 3; i < 9; i++)
            {
                Number = Number * 10 + recieved[i];
            }
            Datetime = new DateTime(recieved[9] * 100 + recieved[10], recieved[11], recieved[12],
                recieved[13], recieved[14], recieved[15]);
            for (byte i = 16; i < 22; i++)
            {
                Sender = Sender * 10 + recieved[i];
            }
            for (byte i = 22; i < 28; i++)
            {
                Channel = Channel * 10 + recieved[i];
            }
            List<byte> text = new List<byte>();
            for (short i = 28; i < 2028; i++)
            {
                text.Add(recieved[i]);
            }
            Text = text.ToStr();
            //Log($"message <{Number}> is had recieved");
        }
    }
}
