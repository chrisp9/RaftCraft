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

        public ClientWebSocket WebSocket { get; private set; }
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

        public async Task PostResponse(RaftMessage message)
        {
            byte[] result;

            lock (this)
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, message);
                    result = stream.ToArray();
                }
            }

            await SendMessageAsync(result);

        }

        public void Start()
        {
         if (WebSocket == null)
            {
                WebSocket = new ClientWebSocket();
                WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
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
                if (WebSocket == null)
                    return WebSocketState.None;

                return WebSocket.State;
            }
        }

        private async Task SendMessageAsync(byte[] message)
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open)
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
                await WebSocket.SendAsync(new ArraySegment<byte>(message, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
            }
        }

        private async void ConnectAsync()
        {
            try
            {
                await WebSocket.ConnectAsync(_uri, _cancellationToken);
                OnConnected?.Invoke();
                StartListen();
            }
            catch (Exception e)
            {
                WebSocket?.Dispose();
                OnError?.Invoke(e);
            }
        }

        private async void DisconnectAsync()
        {
            if (WebSocket != null)
            {
                if (WebSocket.State != WebSocketState.Open)
                {
                    try
                    {
                        await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception during socket close: " + e);
                    }
                }
                WebSocket.Dispose();
                OnDisconnected?.Invoke();
            }
        }

        private async void StartListen()
        {
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (WebSocket.State == WebSocketState.Open)
                {
                    byte[] byteResult = new byte[0];

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

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