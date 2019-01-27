namespace RaftCraft.IntegrationTests.Shims

open RaftCraft.Interfaces
open RaftCraft.Transport
open System.Net.WebSockets
open RaftCraft.IntegrationTests.Common

type PersistentWebSocketClientShim(client : PersistentWebSocketClient) =
    member __.WaitUntilConnected() =
        Poller.Until(fun() -> client.WebSocketState = WebSocketState.Open)

    interface IRaftPeer with
        member __.Post(message) = client.Post(message)
        member __.Start() = client.Start()
