module RaftCraft.ElectionTimer

open System.Timers

type ElectionTimer(electionTimerInterval : float) =
    let createTimer() =
        let timer = new Timer(electionTimerInterval)
        timer.AutoReset <- false
        timer

    let timer = createTimer()
    let observable = timer.Elapsed

    member this.Observable() = observable

    member this.Start() =
        timer.Start()
        ()
    
    member this.Stop() = timer.Stop()