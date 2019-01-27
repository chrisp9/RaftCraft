namespace RaftCraft.IntegrationTests.Framework

open System

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    let getNode nodeId =
        let (_, node) = values.[nodeId - 1]
        node

    let forEachNode selector = values |> List.iter(fun v -> selector(v |> snd))
    let map selector = values |> List.map(fun v -> selector(v |> snd))

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

    member __.Kill(nodeId) =
        forEachNode(fun v -> v.Kill(nodeId))

    member __.Resurrect(nodeId) =
        forEachNode(fun v -> v.Resurrect(nodeId))
    
    member __.AdvanceToElectionTimeout() =
        let tasks = map(fun v -> v.AdvanceToElectionTimeout())
        
        tasks 
            |> Async.Parallel 
            |> Async.RunSynchronously 
            |> ignore

    interface IDisposable with
        member __.Dispose() =
            forEachNode(fun v -> v.Stop())