module Subscription

open System

type Election() = class end

type Subscription<'a>(underlyingDisposable : IDisposable, reset : unit -> unit) =
    let dispose() = underlyingDisposable.Dispose()

    member this.Dispose() = dispose()
    member this.Reset() = reset()

    interface IDisposable with
        member __.Dispose() = dispose()

    static member subscribe<'b> (observe : 'b -> unit) (reset : unit -> unit) (observable : IObservable<'b>) =
        new Subscription<'a>(observable.Subscribe(observe), reset)