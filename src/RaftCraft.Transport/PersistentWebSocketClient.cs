using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RaftCraft.Transport
{
    public class PersistentWebSocketClient : IRaftPeer
    {
        private readonly RaftPeer _peer;
        private readonly Func<RaftPeer, TransientWebSocketClient> _clientFactory;
        private TransientWebSocketClient _currentClient;
        private readonly ILogger _log;

        public PersistentWebSocketClient(
            RaftPeer peer, 
            Func<RaftPeer, TransientWebSocketClient> clientFactory,
            ILogger log)
        {
            _peer = peer;
            _clientFactory = clientFactory;
            _log = log;
        }

        public void Post(RaftMessage message)
        {
            // TODO handle exceptions properly.
            _log.Info("Sending request: " + message);

            if(_currentClient.WebSocket.State == System.Net.WebSockets.WebSocketState.Closed || 
               _currentClient.WebSocket.State == System.Net.WebSockets.WebSocketState.Aborted)
            {
                ErrorHandler(null);
                return;
            }

            _currentClient.PostResponse(message);
        }

        public void Start()
        {
            if(_currentClient != null)
                _currentClient.OnError -= ErrorHandler;

            _currentClient = _clientFactory(_peer);

            _currentClient.OnError += ErrorHandler;

            _currentClient.Start();
        }

        private Task _errorTask = null;

        private void ErrorHandler(Exception e)
        {
            // Slight delay here to avoid swamping unresponsive peers.
            if(_errorTask == null || (_errorTask.IsCompleted || _errorTask.IsFaulted))
                _errorTask = Task.Delay(1000).ContinueWith(_ => Start());
        }
    }
}
