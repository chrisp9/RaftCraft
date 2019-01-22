﻿namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Transport
open RaftCraft.Interfaces
open System
open RaftCraft.Logging
open RaftCraft.Persistence
open RaftCraft.Domain
open RaftTimer
open Utils

type TestLogger() =
    interface ILogger with
        member __.Debug(text) = printfn "%s" text
        member __.Error(text, e) = printfn "%s, Exception: %s" (text) (e.ToString())
        member __.Warn(text) = printfn "%s" text
        member __.Info(text) = printfn "%s" text

type RaftTestSystem(config : RaftConfiguration) =
    let socketFactory = Func<RaftPeer,_>(fun v -> TransientWebSocketClient.Create(v.Address))

    let mutable globalTimer = None

    let forceGetGlobalTimer() = 
        match globalTimer with
            | Some v -> v
            | None -> failwith "Cannot retrieve global timer before it has been created"

    let globalTimerFun = fun v ->
        match globalTimer with
            | Some v -> v
            | None ->
                let timer = new FakeGlobalTimer(v)
                globalTimer <- Some timer
                timer

    let node = RaftSystem.RaftSystem.Create(
                Func<RaftHost, _>(fun host -> new RaftServer(host.Address) :> IRaftHost),
                Func<_,_>(fun peer -> new PersistentWebSocketClient(peer, socketFactory, Log.Instance) :> IRaftPeer),
                new SlowInMemoryDataStore() :> IPersistentDataStore,
                Func<_,_>(fun v -> globalTimerFun(v) :> IGlobalTimer),
                config)

    member __.Node = node

    member __.State = node.State()

    member __.GlobalTimer = forceGetGlobalTimer()

    member __.Tick() = forceGetGlobalTimer().Tick()

    member __.AdvanceTime(milliseconds) =
        let granularity =  config.GlobalTimerTickInterval
        let ticksToPerform = milliseconds / granularity
        
        for _ in 1..ticksToPerform do forceGetGlobalTimer().Tick()
    
    member __.AdvanceToElectionTimeout() =
        let electionTimeout = config.ElectionTimeout
        let granularity = config.GlobalTimerTickInterval

        let fuzzFactor = config.GlobalTimerTickInterval / 2

        let tickCount = TimerUtils.CalculateExpiryTick  (int64 electionTimeout) (int64 granularity) (int64 fuzzFactor)

        for _ in int64 1..tickCount do
            forceGetGlobalTimer().Tick()