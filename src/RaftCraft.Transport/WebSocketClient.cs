using ProtoBuf;
using RaftCraft.Domain;
using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RaftCraft.Transport
{
    public class TransientWebSocketClient
    {
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private ClientWebSocket _ws;
        private readonly Uri _uri;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        public event Action OnConnected;
        public event Action<Exception> OnError;
        private Action<byte[]> OnMessage;
        private Action OnDisconnected;

        public static TransientWebSocketClient Create(string uri)
        {
            return new TransientWebSocketClient(uri + "/raft");
        }

        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public async Task PostResponse(RaftMessage message)
        {
            byte[] result;

            lock (this)
            {
                sw.Start();

                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, message);
                    result = stream.ToArray();
                }
                sw.Stop();
            }

            await SendMessageAsync(result);

        }

        public void Start()
        {
         if (_ws == null)
            {
                _ws = new ClientWebSocket();
                _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            }

            ConnectAsync();
        }

        private TransientWebSocketClient(string uri)
        {
            _uri = new Uri(uri);
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public TransientWebSocketClient Disconnect()
        {
            DisconnectAsync();
            return this;
        }

        /// <summary>
        /// Get the current state of the WebSocket client.
        /// </summary>
        public WebSocketState State
        {
            get
            {
                if (_ws == null)
                    return WebSocketState.None;

                return _ws.State;
            }
        }

        private async Task SendMessageAsync(byte[] message)
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                return;
            }

            var messagesCount = (int)Math.Ceiling((double)message.Length / SendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > message.Length)
                {
                    count = message.Length - offset;
                }
                await _ws.SendAsync(new ArraySegment<byte>(message, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
            }
        }

        private async void ConnectAsync()
        {
            try
            {
                await _ws.ConnectAsync(_uri, _cancellationToken);
                OnConnected?.Invoke();
                StartListen();
            }
            catch (Exception e)
            {
                _ws.Dispose();
                _ws = null;
                OnError?.Invoke(e);
            }
        }

        private async void DisconnectAsync()
        {
            if (_ws != null)
            {
                if (_ws.State != WebSocketState.Open)
                {
                    try
                    {
                        await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception during socket close: " + e);
                    }
                }
                _ws.Dispose();
                _ws = null;

                OnDisconnected?.Invoke();
            }
        }

        private async void StartListen()
        {
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    byte[] byteResult = new byte[0];

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Disconnect();
                        }
                        else
                        {
                            byteResult = byteResult.Concat(buffer.Take(result.Count)).ToArray();
                        }

                    } while (!result.EndOfMessage);

                    OnMessage?.Invoke(byteResult);
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }
    }
}