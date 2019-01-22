namespace RaftCraft

open System
open RaftTimer
open Subscription
open RaftCraft.RaftDomain
open RaftCraft.Logging
open RaftCraft.Domain
open Utils

type TimerExpiry(expiry) =
    let mutable currentExpiry : int64 = expiry
    member __.Expiry = currentExpiry
    member __.Reset(expiry) = currentExpiry <- expiry

type ElectionTimer(timer : GlobalTimerHolder, electionTimerTimeout : int64) =
    let rng = Random()

    let getNextExpiry() =
        let tick = timer.CurrentTick
        let fuzzFactor = int64 (rng.Next(int tick.Granularity) / 2) 

        let offset = TimerUtils.CalculateExpiryTick electionTimerTimeout tick.Granularity fuzzFactor
        let scheduledTick = tick.CurrentTick + offset
        scheduledTick

    let timerExpiry = TimerExpiry(getNextExpiry())

    let expiryCheck (tick : TimerTick) =
        match timerExpiry with
            | v when v.Expiry <= tick.CurrentTick -> true
            | _ -> false

    let reset() =
        let expiry = getNextExpiry()

        let previousExpiry = timerExpiry.Expiry

        Log.Instance.Debug <| sprintf "Resetting ElectionTimer. CurrentTick is %s PreviousExpiry was %s, NewExpiry is %s" (timer.CurrentTick.CurrentTick.ToString()) (previousExpiry.ToString()) (expiry.ToString())

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
        stop()

        Log.Instance.Debug("Election Timer is Starting")
         
        let currentTimer = timer()

        electionTimer <- Some currentTimer
        electionTimerSubscription <- Some (currentTimer.Subscribe 
            (fun tick -> 
                let logLine = sprintf "Election Timer is firing at tick: %s" (tick.CurrentTick.ToString())

                Log.Instance.Debug(logLine)
                stop()
                onFired(tick)))

    member __.Reset() =
        Log.Instance.Debug("Election Timer is resetting")

        match electionTimerSubscription with
            | Some value -> value.Reset()
            | None -> Log.Instance.Debug("ElectionTimer was reset with no active subscription")