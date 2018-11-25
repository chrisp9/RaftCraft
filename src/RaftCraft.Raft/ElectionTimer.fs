module RaftCraft.ElectionTimer

open System.Threading
open System.Timers
open System

type TimerTick() = 
    // TODO Overflow considerations(?)
    let mutable ticks = ref (int64 0)

    member __.Tick() =
        Interlocked.Increment(ticks)

    member __.Ticks = ticks.Value

type ElectionTimer(electionTimerTimeout : int64) =
    let ticks = TimerTick() 
    let rng = Random()

    // So the timer ticks (checks for election timeout) every 50ms
    let electionTimerGranularity = int64 50

    let createTimer() =
        let timer = new Timer(float electionTimerGranularity)
        timer.AutoReset <- true
        timer

    let timer = createTimer()
    let observable = timer.Elapsed 
    let insideStateTransition = ref 0
    let mutable reset = false
    let mutable nextExpiry : int64 option = Option.None

    let stateTransition() =
        // Interlocked needed in case we have two overlapping timer ticks. Ticks happen on the thread pool so this could happen.
        // If this happens, we want the second tick to early out so that we don't have corrupt state.
        if Interlocked.CompareExchange(insideStateTransition, 1, 0) = 0 then
            let currentTicks = ticks.Tick() 

            // First we process queued election timer resets, then we remove any expired peers.
            try
                if reset then
                    // The the expiry tick is calculated as currentTicks + the number of timer granularities 
                    // + a random fuzz factor to avoid split election results.
                    let fuzzFactor = int64 (rng.Next(int electionTimerGranularity) / 5)
                    let expiry = currentTicks + (electionTimerTimeout / electionTimerGranularity) + fuzzFactor
                    nextExpiry <- Some expiry
                    reset <- false
                
                match nextExpiry with
                    | Some v when v <= currentTicks -> 
                        nextExpiry <- Option.None
                        true
                    | _ -> false

            finally
                Interlocked.Exchange(insideStateTransition, 0) |> ignore
        else
            false

    member __.Observable() = 
        observable
        |> Observable.map(fun _ -> stateTransition())
        |> Observable.filter(fun expiredPeer -> expiredPeer)
    
    member __.ResetTimer() =
        reset <- true

    member __.Start() =
        timer.Start()
    
    member __.Stop() = 
        timer.Stop()