namespace RaftCraft

open System
open RaftTimer
open Subscription

type TimerExpiry(expiry) =
    let mutable currentExpiry : int64 = expiry
    member __.Expiry = expiry
    member __.Reset(expiry) = currentExpiry <- expiry

type ElectionTimer(timer : GlobalTimerHolder, electionTimerTimeout : int64) =
    let rng = Random()

    let getNextExpiry() =
        let tick = timer.CurrentTick
        let fuzzFactor = int64 (rng.Next(int tick.Granularity) / 5)
        let scheduledTick = (tick.CurrentTick + (electionTimerTimeout / tick.Granularity) + fuzzFactor)
        scheduledTick

    let timerExpiry = TimerExpiry(getNextExpiry())

    let expiryCheck (tick) =
        match timerExpiry with
            | v when v.Expiry <= tick.CurrentTick -> true
            | _ -> false

    member __.Reset() =
        timerExpiry.Reset(getNextExpiry())

    member __.Subscribe f =
        timer.Observable() 
            |> Observable.filter(expiryCheck) 
            |> Subscription<Election>.subscribe f (fun() -> timerExpiry.Reset(getNextExpiry()))

type ElectionTimerHolder(timer : unit -> ElectionTimer) =
    
    let mutable electionTimer = Option.None
    let mutable electionTimerSubscription : Subscription<Election> option = Option.None

    let stop() = 
        match electionTimerSubscription with
            | Some v -> v.Dispose()
            | None -> ()

        electionTimer <- None
        electionTimerSubscription <- None

    member this.Stop() =
        stop()

    member __.Start(onFired : TimerTick -> unit) =
        stop()
        let currentTimer = timer()

        electionTimer <- Some currentTimer
        electionTimerSubscription <- Some (currentTimer.Subscribe 
            (fun tick -> 
                stop()
                onFired(tick)))

    member __.Reset() =
        match electionTimerSubscription with
            | Some value -> value.Reset()
            | None -> ()