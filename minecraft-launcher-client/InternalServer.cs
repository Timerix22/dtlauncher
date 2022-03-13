using DTLib;
using DTLib.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace launcher_client
{
    class InternalServer
    {
        Socket internalServerSocket;
        Thread internalServerThread;
        public InternalServer()
        {
            internalServerSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            internalServerThread = new(() => Start());
            internalServerThread.Start();
        }

        void Start()
        {
            internalServerSocket.Bind(new IPEndPoint(IPAddress.Loopback, 38888));
            internalServerSocket.Listen(1000);
            if (Launcher.debug) Launcher.Log("g", "internal server started\n");
            var handlerSocket = internalServerSocket.Accept();
            if (Launcher.debug) Launcher.Log("b", "new connection\n");
            while (true)
            {
                try
                {
                    while (true)
                    {
                        if (handlerSocket.Available >= 2)
                        {
                            var request = handlerSocket.GetPackage().ToString();
                            switch (request)
                            {
                                case "client connecting":
                                    Launcher.Log("c", $"request from client: <{request}>\n");
                                    break;
                                default:
                                    throw new Exception("unknown request: " + request);
                            }
                        }
                        else Thread.Sleep(10);
                    }
                }
                catch (ThreadAbortException)
                {
                    handlerSocket.Shutdown(SocketShutdown.Both);
                    handlerSocket.Close();
                    return;
                }
                catch (Exception ex)
                {
                    Launcher.Log("y", $"InternalServer.Start() error:\n{ex.Message}\n{ex.StackTrace}\n");
                }
            }
        }

        public void Stop()
        {
            if (internalServerSocket.Connected) internalServerSocket.Shutdown(SocketShutdown.Both);
            internalServerSocket.Close();
            internalServerThread.Abort();
            if (Launcher.debug) Launcher.Log("g", "internal server stopped\n");
        }
    }
}
