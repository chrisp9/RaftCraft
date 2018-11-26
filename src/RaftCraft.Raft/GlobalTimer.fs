﻿module RaftTimer

open System.Threading
open System.Timers
open System

[<Struct>]
type TimerTick = { Granularity : int64; CurrentTick : int64 }

type GlobalTimer(timerGranularity : int64) =
    let createTimer() =
        let timer = new Timer(float timerGranularity)
        timer.AutoReset <- true
        timer

    let token = ref (Object())
    let event = new Event<unit>()

    let timer = createTimer()
    
    // The indirection here is because we don't push the burden of handling overlapping
    // timer ticks onto consumers. So we ensure that overlapping timer ticks early out.
    let handler = ElapsedEventHandler(fun _ _ -> 
        try
            if Monitor.TryEnter(token) then
                event.Trigger()
        finally
            Monitor.Exit(token))

    let _ = timer.Elapsed.AddHandler(handler)

    let createTick currentTick = {TimerTick.Granularity = timerGranularity; TimerTick.CurrentTick = int64 currentTick}

    member __.Observable() =
        event.Publish
        |> Observable.scan(fun count _ -> createTick (count.CurrentTick + (int64 1))) (createTick(int64 0))

    member __.Start() = timer.Start()

    member __.Stop() = 
        timer.Stop()
        timer.Elapsed.RemoveHandler(handler)

type GlobalTimerHolder(raftTimerFactory : Func<int64, GlobalTimer>, timerGranularity : int64) =
    
    let timer = raftTimerFactory.Invoke(timerGranularity)
    let observable = timer.Observable()
    let mutable currentTick = { TimerTick.Granularity = timerGranularity; TimerTick.CurrentTick = int64 0 }

    let currentValueSubscription = observable.Subscribe(fun value -> currentTick <- value)

    member __.Observable() = observable
    member __.CurrentTick = currentTick
    member __.Start() = timer.Start()
    member __.Stop() = 
        timer.Stop()
        currentValueSubscription.Dispose()