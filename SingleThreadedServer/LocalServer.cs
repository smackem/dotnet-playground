using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SingleThreadedServer
{
    class LocalServer : IDisposable
    {
        readonly Socket _listenSocket;
        readonly List<RemoteClient> _clients = new List<RemoteClient>();
        readonly IDictionary<Socket, RemoteClient> _clientsBySocket = new Dictionary<Socket, RemoteClient>();

        public LocalServer(int port)
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _listenSocket.Listen(10);
        }

        internal void CloseClient(RemoteClient client)
        {
            _clients.Remove(client);
            _clientsBySocket.Remove(client.Socket);
            client.Dispose();

            Console.WriteLine($"!! {client} disconnected");
        }

        public void SendToAll(string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            var disconnectedClients = new List<RemoteClient>();

            foreach (var client in _clients)
            {
                try
                {
                    client.Send(bytes, 0, bytes.Length);
                }
                catch (Exception)
                {
                    disconnectedClients.Add(client);
                }
            }

            foreach (var client in disconnectedClients)
                CloseClient(client);
        }

        public void Run()
        {
            var buffer = new byte[16 * 1024];
            var readableSockets = new List<Socket>(64);
            var errorSockets = new List<Socket>(64);

            while (true)
            {
                readableSockets.Clear();
                errorSockets.Clear();

                readableSockets.Add(_listenSocket);

                foreach (var client in _clients)
                {
                    readableSockets.Add(client.Socket);
                    errorSockets.Add(client.Socket);
                }

                Socket.Select(readableSockets, null, errorSockets, -1);

                foreach (var readableSocket in readableSockets)
                {
                    if (readableSocket == _listenSocket)
                    {
                        var socket = readableSocket.Accept();
                        var client = new RemoteClient(this, socket);

                        AddClient(client);
                    }
                    else
                    {
                        if (_clientsBySocket.TryGetValue(readableSocket, out var client))
                        {
                            var count = 0;

                            try
                            {
                                count = readableSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                            }
                            catch (Exception)
                            {
                                CloseClient(client);
                            }

                            if (count > 0)
                                client.Read(buffer, 0, count);
                        }
                    }
                }

                foreach (var errorSocket in errorSockets)
                {
                    var client = _clientsBySocket[errorSocket];
                    CloseClient(client);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _listenSocket.Dispose();
            }
            catch (Exception)
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////

        void AddClient(RemoteClient client)
        {
            _clients.Add(client);
            _clientsBySocket[client.Socket] = client;

            Console.WriteLine($"!! {client} connected");
        }
    }
}
