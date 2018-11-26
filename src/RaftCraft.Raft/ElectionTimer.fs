module RaftCraft.ElectionTimer

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
        (tick.CurrentTick + (electionTimerTimeout / tick.Granularity) + fuzzFactor)

    let timerExpiry = TimerExpiry(getNextExpiry())

    let expiryCheck (tick) =
        match timerExpiry with
            | v when v.Expiry <= tick.CurrentTick -> true
            | _ -> false

    member __.Subscribe f =
        timer.Observable() 
            |> Observable.filter(expiryCheck) 
            |> Subscription<ElectionSubscription>.Subscribe(f)

    member __.Reset() =
        timerExpiry.Reset(getNextExpiry())