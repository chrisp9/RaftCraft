namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Transport
open RaftCraft.Interfaces
open System
open RaftCraft.Logging
open RaftCraft.Persistence
open RaftCraft.Domain

type TestLogger() =
    interface ILogger with
        member __.Debug(text) = printfn "%s" text
        member __.Error(text, e) = printfn "%s, Exception: %s" (text) (e.ToString())
        member __.Warn(text) = printfn "%s" text
        member __.Info(text) = printfn "%s" text

type RaftTestSystem(config : RaftConfiguration) =
    let socketFactory = Func<RaftPeer,_>(fun v -> TransientWebSocketClient.Create(v.Address))

    let node = RaftSystem.RaftSystem.Create(
                Func<RaftHost, _>(fun host -> new RaftServer(host.Address) :> IRaftHost),
                Func<_,_>(fun peer -> new PersistentWebSocketClient(peer, socketFactory, Log.Instance) :> IRaftPeer),
                new SlowInMemoryDataStore() :> IPersistentDataStore,
                Func<_,_>(fun v -> new FakeGlobalTimer(v) :> IGlobalTimer),
                config)

    member __.Node = node
    member __.State = node.State()