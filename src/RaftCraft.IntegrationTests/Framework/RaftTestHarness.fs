namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Domain

type RaftTestHarness(numberOfNodes : int) =
    let basePortNumber = 24500

    let getAddress port = sprintf "localhost:%s" (port.ToString())

    let peer nodeId = RaftPeer(nodeId, getAddress (basePortNumber + nodeId))

    let peers current total= [ for i in 1..total do if current <> i then yield i ]

    let getConfigFor nodeId =
        let address = getAddress(basePortNumber + nodeId)
        
        let peersForNode = 
            peers nodeId numberOfNodes 
                |> Seq.map peer
                |> Array.ofSeq

        RaftConfiguration(RaftHost(nodeId, address), peersForNode, "", 50, 500, 500)

    member __.Initialize() =
        [for nodeId in [1..numberOfNodes] do yield RaftTestSystem (getConfigFor nodeId)]