using Fleck;
using ProtoBuf;
using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System;
using System.IO;

namespace RaftCraft.Transport
{
    public class RaftServer : IRaftHost
    {
        private readonly string _address;
        private WebSocketServer _webSocketServer;

        public RaftServer(string address)
        {
            _address = address;
            _webSocketServer = new WebSocketServer(_address);
        }

        public void Start(Action<RequestMessage> onMessage)
        {
            _webSocketServer.Start(socket =>
            {
                socket.OnBinary = message =>
                {
                    RequestMessage result;

                    // TODO pool streams?
                    using (var memoryStream = new MemoryStream())
                    {
                        result = Serializer.Deserialize<RequestMessage>(memoryStream);
                    }

                    onMessage?.Invoke(result);
                };
            });
               
        }
    }
}
