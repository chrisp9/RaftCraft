namespace RaftCraft.IntegrationTests.Shims

open RaftCraft.Interfaces
open RaftCraft.Transport
open System.Net.WebSockets
open RaftCraft.IntegrationTests.Common

type PersistentWebSocketClientShim(client : PersistentWebSocketClient) =
    let mutable canPost = true
    
    member __.WaitUntilConnected() =
        Poller.Until(fun() -> client.WebSocketState = WebSocketState.Open)

    member __.Kill() = canPost <- false
    member __.Resurrect() = canPost <- true

    interface IRaftPeer with
        member __.Post(message) = if canPost then client.Post(message)
        member __.Start() = client.Start()