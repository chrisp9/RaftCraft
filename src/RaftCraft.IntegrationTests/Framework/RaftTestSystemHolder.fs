namespace RaftCraft.IntegrationTests.Framework

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    member __.GetNode(nodeId : NodeId) = values.[nodeId]

    member __.Tick(nodeId : NodeId) =
       let (_, node) = values.[nodeId]
       node.Node