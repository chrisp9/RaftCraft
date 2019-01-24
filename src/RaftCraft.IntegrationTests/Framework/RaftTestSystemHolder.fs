namespace RaftCraft.IntegrationTests.Framework

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    let getNode nodeId =
        let (_, node) = values.[nodeId - 1]
        node

    let forEachNode selector = values |> List.iter(fun v -> selector(v |> snd))
    
    member __.GetNode(nodeId : NodeId) = 
        getNode nodeId

    member this.Start() =
        values |> List.iter(fun v -> (v |> snd).Start())
        this

    member __.Tick(nodeId : NodeId) =
        (getNode nodeId).GlobalTimer.Tick()

    member __.TickAll() = forEachNode(fun v -> v.Tick())

    member __.AdvanceTime(milliseconds) =
        forEachNode(fun v -> v.AdvanceTime(milliseconds))

    member __.AdvanceToElectionTimeout() =
        forEachNode(fun v -> v.AdvanceToElectionTimeout())