namespace RaftCraft.IntegrationTests.Framework

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    let getNode nodeId =
        let (_, node) = values.[nodeId]
        node
    
    member __.GetNode(nodeId : NodeId) = 
        getNode nodeId

    member __.Tick(nodeId : NodeId) =
        (getNode nodeId).GlobalTimer.Tick()

    member __.TickAll() =
        values |> List.iter(fun v -> (v |> snd).Tick())

    member __.AdvanceTime(milliseconds) =
        values |> List.iter(fun v -> (v |> snd).AdvanceTime(milliseconds))

    member __.AdvanceToElectionTimeout() =
        values |> List.iter(fun v -> (v |> snd).AdvanceToElectionTimeout())