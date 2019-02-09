namespace RaftCraft.IntegrationTests.Framework

open System
open RaftCraft.IntegrationTests.Common

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    let getNode nodeId =
        let (_, node) = values.[nodeId - 1]
        node

    let forEachNode selector = values |> List.iter(fun v -> selector(v |> snd))
    let map selector = values |> List.map(fun v -> selector(v |> snd))
    let map2 selector = values |> List.map(fun v -> (v |> snd, selector(v |> snd)))

    let matches func =
        (map2 func) 
            |> Seq.filter(fun (a, b) -> b = true)
            |> Seq.map(fun (a, _) -> a)

    let any func = 
        let matched = matches func |> List.ofSeq

        match matched with
            | x :: y -> Some x
            | [] -> None

    let exactlyOne func =
        let matched = matches func |> List.ofSeq
        
        match matched with
            | x :: y when y.Length = 0 -> Some x
            | x :: y -> failwith("Expected only one element")
            | [] -> None

    member __.GetNode(nodeId : NodeId) = 
        getNode nodeId

    member this.Start() =
        forEachNode(fun v -> v.Start())
        forEachNode(fun v -> v.WaitUntilConnected())
        this

    member __.Tick(nodeId : NodeId) =
        (getNode nodeId).GlobalTimer.Tick()

    member __.TickAll() = forEachNode(fun v -> v.Tick())

    member __.AdvanceTime(milliseconds) =
        forEachNode(fun v -> v.AdvanceTime(milliseconds))

    member __.KillCommunicationWith(nodeId) =
        forEachNode(fun v -> v.Kill(nodeId))

    member __.ResurrectCommunicationWith(nodeId) =
        forEachNode(fun v -> v.Resurrect(nodeId))

    member __.ExpectTerm(term) =
        Poller.UntilMatch(fun() -> any (fun v -> v.State.Term = term))

    interface IDisposable with
        member __.Dispose() =
            forEachNode(fun v -> v.Stop())