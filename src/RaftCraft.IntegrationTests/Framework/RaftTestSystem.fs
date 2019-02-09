namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Transport
open RaftCraft.Interfaces
open System
open RaftCraft.Logging
open RaftCraft.Persistence
open RaftCraft.Domain
open RaftTimer
open Utils
open RaftCraft.IntegrationTests.Shims
open RaftCraft.RaftDomain

type TestLogger() =
    interface ILogger with
        member __.Debug(text) = printfn "%s" text
        member __.Error(text, e) = printfn "%s, Exception: %s" (text) (e.ToString())
        member __.Warn(text) = printfn "%s" text
        member __.Info(text) = printfn "%s" text

type RaftTestSystem(config : RaftConfiguration) =
    let socketFactory = Func<RaftPeer,_>(fun v -> TransientWebSocketClient.Create(v.Address))

    let globalTimerShim = Shim<FakeGlobalTimer>()
    let persistentWebSocketClientShim = KeyedShim<int, PersistentWebSocketClientShim>()

    let create (peer : RaftPeer) =
        persistentWebSocketClientShim.Create (peer.NodeId) (fun _ -> new PersistentWebSocketClient(peer, socketFactory, Log.Instance) |> PersistentWebSocketClientShim)

    let node = RaftSystem.RaftSystem.Create(
                Func<RaftHost, _>(fun host -> new RaftServer(host.Address) :> IRaftHost),
                Func<_,_>(fun peer -> create peer :> IRaftPeer),
                new SlowInMemoryDataStore() :> IPersistentDataStore,
                Func<_,_>(fun v -> globalTimerShim.Create(fun () ->  new FakeGlobalTimer(v)) :> IGlobalTimer),
                config)

    member __.Node = node

    member __.State = node.State()

    member __.GlobalTimer = globalTimerShim.ForceGet()

    member __.Tick() = globalTimerShim.ForceGet().Tick()

    member __.Start() = node.Start()

    member __.WaitUntilConnected() =
        persistentWebSocketClientShim.ForAll (fun _ v -> v.WaitUntilConnected())

    member __.Kill(nodeId) =
        persistentWebSocketClientShim.ForAll (fun k v -> if k = nodeId then v.Kill())

    member __.Resurrect(nodeId) =
        persistentWebSocketClientShim.ForAll (fun k v -> if k = nodeId then v.Resurrect())

    member __.AdvanceTime(milliseconds) =
        let granularity =  config.GlobalTimerTickInterval
        let ticksToPerform = milliseconds / granularity
        
        for _ in 1..ticksToPerform do globalTimerShim.ForceGet().Tick()
    
    member __.AdvanceToCandidate() =
        async {
            
            while node.State().RaftRole <> RaftRole.Candidate do
                globalTimerShim.ForceGet().Tick()
                let! _ =  node.Ping()
                ()
        }

    member __.Stop() = node.Stop()