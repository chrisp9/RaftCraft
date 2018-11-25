module RaftCraft.ElectionTimer

open System.Threading
open System.Timers
open RaftCraft.Interfaces
open System.Collections.Generic
open System
open System.Collections.Concurrent

type TimerTick() = 
    // TODO Overflow considerations(?)
    let mutable ticks = ref (int64 0)

    member __.Tick() =
        Interlocked.Increment(ticks)

    member __.Ticks = ticks.Value

type ElectionTimer(electionTimerTimeout : int64) =
    let peerExpiries = Dictionary<IRaftPeer, int64>()
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
    let timerResetQueue = ConcurrentQueue<IRaftPeer>()

    let stateTransition() =
        // Interlocked needed in case we have two overlapping timer ticks. Ticks happen on the thread pool so this could happen.
        // If this happens, we want the second tick to early out so that we don't have corrupt state.
        if Interlocked.CompareExchange(insideStateTransition, 1, 0) = 0 then
            let currentTicks = ticks.Tick() 

            // First we process queued election timer resets, then we remove any expired peers.
            try
                while timerResetQueue.Count > 0 do
                    let success, peer = timerResetQueue.TryDequeue()
                    if success then
                        // The the expiry tick is calculated as currentTicks + the number of timer granularities 
                        // + a random fuzz factor to avoid split election results.
                        let fuzzFactor = int64 (rng.Next(int electionTimerGranularity) / 5)
                        
                        let expiry = currentTicks + (electionTimerTimeout / electionTimerGranularity) + fuzzFactor
                        peerExpiries.[peer] <- currentTicks + expiry

                let itemsToRemove = 
                    peerExpiries 
                    |> Dict.tryRemove(fun expiryCandidate -> expiryCandidate.Value <= currentTicks)
                
                itemsToRemove |> Option.map(fun v -> v.Key)
            finally
                Interlocked.Exchange(insideStateTransition, 0) |> ignore
        else
            None

    member __.Observable() = 
        // NOTE Only one item can expire per tick granulariy (primarily to avoid excessive allocations each expiry)
        // This means a sensible (small) tick granularity should be chosen to avoid this causing problems
        // Also this observable isn't pure because it mutates tick state. However, it exposes a clean interface to
        // consumers. For large numbers of peers, it won't be efficient and would require a rethink (ie. keying by
        // expiry time). 
        // We need the mutability for performance reasons (we want to avoid System Calls for DateTime.UtcNow)
        observable
        |> Observable.map(fun _ -> stateTransition())
        |> Observable.choose(fun expiredPeer -> expiredPeer)
    
    member __.ResetTimer(peer: IRaftPeer) =
        timerResetQueue.Enqueue(peer)

    member __.Start() =
        timer.Start()
    
    member __.Stop() = 
        timer.Stop()