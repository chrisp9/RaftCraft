namespace RaftCraft

open System
open RaftTimer
open Subscription
open RaftCraft.RaftDomain
open RaftCraft.Logging

type TimerExpiry(expiry) =
    let mutable currentExpiry : int64 = expiry
    member __.Expiry = expiry
    member __.Reset(expiry) = currentExpiry <- expiry

type ElectionTimer(timer : GlobalTimerHolder, electionTimerTimeout : int64) =
    let rng = Random()

    let getNextExpiry() =
        let tick = timer.CurrentTick
        let fuzzFactor = int64 (rng.Next(int tick.Granularity) / 4) 
        let scheduledTick = (tick.CurrentTick + (electionTimerTimeout / tick.Granularity) + fuzzFactor)
        scheduledTick

    let timerExpiry = TimerExpiry(getNextExpiry())

    let expiryCheck (tick) =
        match timerExpiry with
            | v when v.Expiry <= tick.CurrentTick -> true
            | _ -> false

    let reset() =
        let expiry = getNextExpiry()

        let previousExpiry = timerExpiry.Expiry

        Log.Instance.Debug <| sprintf "Resetting ElectionTimer. PreviousExpiry was %s, NewExpiry is %s" (previousExpiry.ToString()) (expiry.ToString())

        timerExpiry.Reset(expiry)
    
    member __.Subscribe f =
        timer.Observable() 
            |> Observable.filter(expiryCheck) 
            |> Subscription<Election>.subscribe f (fun() -> reset())

type ElectionTimerHolder(timer : unit -> ElectionTimer) =
    let mutable electionTimer = Option.None
    let mutable electionTimerSubscription : Subscription<Election> option = Option.None

    let stop() = 
        Log.Instance.Debug("Election Timer is stopping")

        match electionTimerSubscription with
            | Some v -> v.Dispose()
            | None -> ()

        electionTimer <- None
        electionTimerSubscription <- None

    member this.Stop() =
        stop()

    member __.Start(onFired : TimerTick -> unit) =
        Log.Instance.Debug("Election Timer is Starting")
        stop()
        let currentTimer = timer()

        electionTimer <- Some currentTimer
        electionTimerSubscription <- Some (currentTimer.Subscribe 
            (fun tick -> 
                Log.Instance.Debug("Election Timer is firing")
                stop()
                onFired(tick)))

    member __.Reset() =
        Log.Instance.Debug("Election Timer is resetting")

        match electionTimerSubscription with
            | Some value -> value.Reset()
            | None -> ()