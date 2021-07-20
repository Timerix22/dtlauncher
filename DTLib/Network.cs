using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static DTLib.PublicLog;
using static DTLib.Filework;

namespace DTLib
{
    // 
    // весь униврсальный неткод тут
    // большинство методов предназначены для работы с TCP сокетами (Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
    //
    public static class Network
    {

        // ждёт пакет заданного размера с заданным началом и концом
        public static byte[] GetPackage(this Socket socket)
        {
            int packageSize = 0;
            byte[] data = new byte[2];
            // цикл выполняется пока не пройдёт 1000 мс
            for (ushort s = 0; s < 200; s += 1)
            {
                if (packageSize == 0 && socket.Available >= 2)
                {
                    socket.Receive(data, data.Length, 0);
                    packageSize = data.BytesToInt();

                }
                if (packageSize != 0 && socket.Available >= packageSize)
                {
                    data = new byte[packageSize];
                    socket.Receive(data, data.Length, 0);
                    return data;
                }
                else Thread.Sleep(5);
            }
            throw new Exception($"GetPackage() error: timeout. socket.Available={socket.Available}\n");
        }

        // отправляет пакет заданного размера, добавля в конец нули если длина data меньше чем packageSize
        public static void SendPackage(this Socket socket, byte[] data)
        {
            if (data.Length > 65536) throw new Exception($"SendPackage() error: package is too big ({data.Length} bytes)");
            if (data.Length == 0) throw new Exception($"SendPackage() error: package has zero size");
            var list = new List<byte>();
            byte[] packageSize = data.Length.IntToBytes();
            if (packageSize.Length == 1) list.Add(0);
            list.AddRange(packageSize);
            list.AddRange(data);
            socket.Send(list.ToArray());
        }


        // скачивает файл с помощью FSP протокола
        public static void FSP_Download(this Socket mainSocket, string filePath_server, string filePath_client)
        {
            Log("g", $"requesting file download: {filePath_server}\n");
            mainSocket.SendPackage("requesting file download".ToBytes());
            mainSocket.SendPackage(filePath_server.ToBytes());
            FSP_Download(mainSocket, filePath_client);
        }

        public static void FSP_Download(this Socket mainSocket, string filePath_client)
        {
            File.Create(filePath_client);
            using var fileStream = File.OpenWrite(filePath_client);
            var fileSize = mainSocket.GetPackage().ToStr().ToUInt();
            var hashstr = mainSocket.GetPackage().HashToString();
            mainSocket.SendPackage("ready".ToBytes());
            int packagesCount = 0;
            byte[] buffer = new byte[5120];
            int fullPackagesCount = SimpleConverter.Truncate(fileSize / buffer.Length);
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
                buffer = mainSocket.GetPackage();
                fileStream.Write(buffer, 0, buffer.Length);
                fileStream.Flush();
            }
            speedCounter.Stop();
            // получение остатка
            if ((fileSize - fileStream.Position) > 0)
            {
                mainSocket.SendPackage("remain request".ToBytes());
                buffer = mainSocket.GetPackage();
                fileStream.Write(buffer, 0, buffer.Length);
            }
            fileStream.Flush();
            fileStream.Close();
            Log(new string[] { "g", $"   downloaded {packagesCount * 5120 + buffer.Length} of {fileSize} bytes\n" });

        }

        // отдаёт файл с помощью FSP протокола
        public static void FSP_Upload(this Socket mainSocket, string filePath)
        {
            Log("b", $"uploading file {filePath}\n");
            using var fileStream = File.OpenRead(filePath);
            var fileSize = File.GetSize(filePath);
            var fileHash = new Hasher().HashFile(filePath);
            mainSocket.SendPackage(fileSize.ToString().ToBytes());
            mainSocket.SendPackage(fileHash);
            if (mainSocket.GetPackage().ToStr() != "ready") throw new Exception("user socket isn't ready");
            byte[] buffer = new byte[5120];
            var hashstr = fileHash.HashToString();
            int packagesCount = 0;
            int seconds = 0;
            // рассчёт скорости
            var speedCounter = new Timer(true, 1000, () =>
            {
                seconds++;
                Log("c", $"speed= {packagesCount * buffer.Length / (seconds * 1000)} kb/s\n");
            });
            // отправка файла
            int fullPackagesCount = SimpleConverter.Truncate(fileSize / buffer.Length);
            for (; packagesCount < fullPackagesCount; packagesCount++)
            {
                fileStream.Read(buffer, 0, buffer.Length);
                mainSocket.SendPackage(buffer);
            }
            speedCounter.Stop();
            // досылка остатка
            if ((fileSize - fileStream.Position) > 0)
            {
                if (mainSocket.GetPackage().ToStr() != "remain request") throw new Exception("FSP_Upload() error: didn't get remain request");
                buffer = new byte[(fileSize - fileStream.Position).ToInt()];
                fileStream.Read(buffer, 0, buffer.Length);
                mainSocket.SendPackage(buffer);
            }
            fileStream.Close();
            Log(new string[] { "g", $"   uploaded {packagesCount * 5120 + buffer.Length} of {fileSize} bytes\n" });
        }

        // получает с сайта публичный ip
        public static string GetPublicIP() => new WebClient().DownloadString("https://ipv4bot.whatismyipaddress.com/");

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

        public class FSP
        {
            Socket mainSocket;
            FSP(Socket _mainSocket) => mainSocket = _mainSocket;

            //public delegate void FSP_LogDelegate(uint bytes);
            public uint BytesDownloaded = 0;
            public uint BytesUploaded = 0;
            public uint Filesize = 0;

            // скачивает файл с помощью FSP протокола
            public void DownloadFile(string filePath_server, string filePath_client)
            {
                Log("g", $"requesting file download: {filePath_server}\n");
                mainSocket.SendPackage("requesting file download".ToBytes());
                mainSocket.SendPackage(filePath_server.ToBytes());
                DownloadFile(filePath_client);
            }

            public void DownloadFile(string filePath_client)
            {
                File.Create(filePath_client);
                using var fileStream = File.OpenWrite(filePath_client);
                Filesize = mainSocket.GetPackage().ToStr().ToUInt();
                var hashstr = mainSocket.GetPackage().HashToString();
                mainSocket.SendPackage("ready".ToBytes());
                int packagesCount = 0;
                byte[] buffer = new byte[5120];
                int fullPackagesCount = SimpleConverter.Truncate(Filesize / buffer.Length);
                // получение файла
                for (; packagesCount < fullPackagesCount; packagesCount++)
                {
                    buffer = mainSocket.GetPackage();
                    fileStream.Write(buffer, 0, buffer.Length);
                    fileStream.Flush();
                }
                // получение остатка
                if ((Filesize - fileStream.Position) > 0)
                {
                    mainSocket.SendPackage("remain request".ToBytes());
                    buffer = mainSocket.GetPackage();
                    fileStream.Write(buffer, 0, buffer.Length);
                }
                fileStream.Flush();
                fileStream.Close();
                Log(new string[] { "g", $"   downloaded {packagesCount * 5120 + buffer.Length} of {Filesize} bytes\n" });
            }

            // отдаёт файл с помощью FSP протокола
            public void UploadFile(string filePath)
            {
                Log("b", $"uploading file {filePath}\n");
                using var fileStream = File.OpenRead(filePath);
                Filesize = File.GetSize(filePath).ToUInt();
                var fileHash = new Hasher().HashFile(filePath);
                mainSocket.SendPackage(Filesize.ToString().ToBytes());
                mainSocket.SendPackage(fileHash);
                if (mainSocket.GetPackage().ToStr() != "ready") throw new Exception("user socket isn't ready");
                byte[] buffer = new byte[5120];
                var hashstr = fileHash.HashToString();
                int packagesCount = 0;
                // отправка файла
                int fullPackagesCount = SimpleConverter.Truncate(Filesize / buffer.Length);
                for (; packagesCount < fullPackagesCount; packagesCount++)
                {
                    fileStream.Read(buffer, 0, buffer.Length);
                    mainSocket.SendPackage(buffer);
                }
                // досылка остатка
                if ((Filesize - fileStream.Position) > 0)
                {
                    if (mainSocket.GetPackage().ToStr() != "remain request") throw new Exception("FSP_Upload() error: didn't get remain request");
                    buffer = new byte[(Filesize - fileStream.Position).ToInt()];
                    fileStream.Read(buffer, 0, buffer.Length);
                    mainSocket.SendPackage(buffer);
                }
                fileStream.Close();
                Log(new string[] { "g", $"   uploaded {packagesCount * 5120 + buffer.Length} of {Filesize} bytes\n" });
            }

            public void DownloadByManifest(string manifestOnServer, string dirOnClient)
            {
                Log("b", "downloading manifest <", "c", manifestOnServer, "b", ">\n");
                DownloadFile(manifestOnServer, "TEMP\\manifestOnServer");
                var manifest = new Dtsod(File.ReadAllText("TEMP\\manifest.dtsod"));
                Log("g", $"found {manifest.Values.Count} files in manifest\n");
                var hasher = new Hasher();
                foreach (string fileOnServer in manifest.Values.Keys)
                {
                    string fileOnClient = $"{dirOnClient}\\{fileOnServer}";
                    Log("b", "file <", "c", fileOnClient, "b", ">...  ");
                    if (!File.Exists(fileOnClient))
                    {
                        LogNoTime("y", "doesn't exist\n");
                        DownloadFile(fileOnServer, fileOnClient);
                    }
                    else if (hasher.HashFile(fileOnClient).HashToString() != manifest[fileOnServer])
                    {
                        LogNoTime("y", "outdated\n");
                        DownloadFile(fileOnServer, fileOnClient);
                    }
                    else LogNoTime("g", "without changes\n");
                }
                Directory.Delete("TEMP");
            }
        }
    }
}
