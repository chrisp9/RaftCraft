namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Domain
open RaftCraft.Interfaces
open System

type FakeGlobalTimer(granularity) =
    let tickEvent = Event<TimerTick>()
    let mutable canPublish = false
    let mutable currentTick = int64 0

    interface IGlobalTimer with
        member __.Observable() = 
            tickEvent.Publish :> IObservable<TimerTick>
        member __.Start() = 
            canPublish <- true
        member __.Stop() = 
            canPublish <- false
     
    member __.Tick() =
        if canPublish then
            currentTick <- currentTick + int64 1
            tickEvent.Trigger (TimerTick(granularity, currentTick))