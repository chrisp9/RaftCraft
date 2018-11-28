using ProtoBuf;
using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RaftCraft.Transport
{
    public class Behavior : WebSocketBehavior
    {
        private Action<RaftMessage> _onMessage;

        public Behavior(Action<RaftMessage> onMessage)
        {
            _onMessage = onMessage;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            // TODO pool streams?
            // TODO pool streams?
            RaftMessage result;
            using (var memoryStream = new MemoryStream(e.RawData))
            {
                result = Serializer.Deserialize<RaftMessage>(memoryStream);
            }

            _onMessage?.Invoke(result);
        }
    }

    public class RaftServer : IRaftHost
    {
        private readonly string _address;
        private WebSocketServer _webSocketServer;

        public RaftServer(string address)
        {
            _address = address;
            _webSocketServer = new WebSocketServer(_address);
        }

        private Action<RaftMessage> _onMessage;

        public void Start(Action<RaftMessage> onMessage)
        {
            _webSocketServer.Start();

            // TODO yuck. Allocation Land.
            _webSocketServer.AddWebSocketService("/raft", () => new Behavior(onMessage));
        }
    }
}
