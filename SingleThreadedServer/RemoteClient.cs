using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SingleThreadedServer
{
    class RemoteClient : IDisposable
    {
        readonly LocalServer _server;
        readonly ResizeArray<byte> _message = new ResizeArray<byte>();
        readonly string _remoteAddress;

        public RemoteClient(LocalServer server, Socket socket)
        {
            Socket = socket;
            _server = server;
            _remoteAddress = Socket.RemoteEndPoint.ToString();
        }

        internal Socket Socket { get; }

        internal void Send(byte[] message, int index, int count)
        {
            Socket.Send(message, index, count, SocketFlags.None);
        }

        internal void Read(byte[] buffer, int index, int count)
        {
            count += index;

            for (int i = index; i < count; i++)
                ReadByte(buffer[i]);
        }

        public void Dispose() =>  Socket.Dispose();

        public override string ToString() => _remoteAddress;

        ///////////////////////////////////////////////////////////////////////

        void ReadByte(byte b)
        {
            if (b == '\n')
            {
                var text = Encoding.ASCII.GetString(_message.ReadOnlyArray, 0, _message.Count);
                _message.Clear();

                var messageToSend = $"{this}: {text}\n";
                Console.Write(messageToSend);
                _server.SendToAll(messageToSend);
            }
            else
            {
                _message.Add(b);
            }
        }
    }
}
