module RaftTimer

open System.Threading
open System.Timers
open System
open RaftCraft.RaftDomain
open RaftCraft.Domain
open RaftCraft.Interfaces

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
        if Monitor.TryEnter(token) then
            try
                event.Trigger()
            finally
                Monitor.Exit(token))

    let _ = timer.Elapsed.AddHandler(handler)

    let createTick currentTick = TimerTick(timerGranularity, int64 currentTick)

    let observable = 
        event.Publish
        |> Observable.scan(fun count _ -> createTick (count.CurrentTick + (int64 1))) (createTick(int64 0))

    interface IGlobalTimer with
        member __.Observable() = observable

        member __.Start() = timer.Start()

        member __.Stop() = 
            timer.Stop()
            timer.Elapsed.RemoveHandler(handler)

// Holds mutable state related to the GlobalTimer (currentTick) which is important for consumers.
type GlobalTimerHolder(raftTimerFactory : int64 -> IGlobalTimer, timerGranularity : int64) =
    
    let timer = raftTimerFactory timerGranularity
    let observable = timer.Observable()
    let mutable currentTick = TimerTick(timerGranularity, int64 0)

    let pushValueObservable = Event<TimerTick>()

    let currentValueSubscription = observable.Subscribe(fun value -> 
        currentTick <- value
        pushValueObservable.Trigger(value))

    member __.Observable() = pushValueObservable.Publish
    member __.CurrentTick = currentTick
    member __.Start() = timer.Start()
    member __.Stop() = 
        timer.Stop()
        currentValueSubscription.Dispose()