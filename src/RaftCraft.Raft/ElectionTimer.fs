module RaftCraft.ElectionTimer

open System.Threading
open System.Timers
open RaftCraft.Interfaces
open System.Collections.Generic

type TimerTick() = 
    // TODO Overflow considerations(?)
    let mutable ticks = ref (int64 0)

    member __.Tick() =
        Interlocked.Increment(ticks)

    member __.Ticks = ticks.Value

type ElectionTimer(electionTimerGranularity : int64, electionTimerTimeout : int64) =
    let peerExpiries = Dictionary<IRaftPeer, int64>()
    let ticks = TimerTick() 
    let rng = Random()

    let createTimer() =
        let timer = new Timer(float electionTimerGranularity)
        timer.AutoReset <- false
        timer

    let timer = createTimer()
    let observable = timer.Elapsed

    member this.Observable() = 
        // NOTE Only one item can expire per tick granulariy (primarily to avoid excessive allocations each expiry)
        // This means a sensible (small) tick granularity should be chosen to avoid this causing problems
        // Also this observable isn't pure because it mutates tick state. However, it exposes a clean interface to
        // consumers. For large numbers of peers, it won't be efficient and would require a rethink.
        observable
        |> Observable.map(fun _ -> ticks.Tick())
        |> Observable.choose(fun tick -> 
            peerExpiries 
            |> Dict.tryRemove(fun expiryCandidate -> expiryCandidate.Value <= tick)
            |> Option.map(fun v -> v.Value))
    
    member this.ResetTimer(peer: IRaftPeer) =
        let currentTicks = ticks.Ticks
        let expiry = electionTimerTimeout / electionTimerGranularity
        peerExpiries.[peer] = currentTicks + expiry |> ignore
        ()

    member this.Start() =
        timer.Start()
        ()
    
    member this.Stop() = timer.Stop()