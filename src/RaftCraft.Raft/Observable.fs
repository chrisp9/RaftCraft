module Subscription

open System

type ElectionSubscription() = class end

type Subscription<'a>(underlyingDisposable : IDisposable) =
    interface IDisposable with
        member __.Dispose() = underlyingDisposable.Dispose()

    static member Subscribe<'b> (observe : 'b -> unit) (observable : IObservable<'b>) =
        new Subscription<'a>(observable.Subscribe(observe))