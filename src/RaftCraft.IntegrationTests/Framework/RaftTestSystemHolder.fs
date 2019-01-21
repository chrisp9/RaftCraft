namespace RaftCraft.IntegrationTests.Framework

type RaftTestSystemHolder(values : ((int*RaftTestSystem) list)) = 
    let getNode nodeId =
        let (_, node) = values.[nodeId]
        node
    
    member __.GetNode(nodeId : NodeId) = 
        getNode nodeId

    member __.Tick(nodeId : NodeId) =
       let (_, node) = values.[nodeId]
       node.Node