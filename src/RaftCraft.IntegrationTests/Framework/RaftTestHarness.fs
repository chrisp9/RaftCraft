﻿namespace RaftCraft.IntegrationTests.Framework

open RaftCraft.Domain

type RaftTestHarness
    (numberOfNodes : int, 
     getTimerTickGranularity : int -> int, 
     getElectionTimeout : int -> int,
     getRetryInterval : int -> int) =

    let basePortNumber = 24500

    let getAddress port = sprintf "localhost:%s" (port.ToString())

    let peer nodeId = RaftPeer(nodeId, getAddress (basePortNumber + nodeId))

    // Returns Peer IDs for all nodes except the current node
    let peers current total = [ for i in 1..total do if current <> i then yield i ]

    let getConfigFor nodeId =
        let address = getAddress(basePortNumber + nodeId)
        
        let peersForNode = 
            peers nodeId numberOfNodes
                |> Seq.map peer
                |> Array.ofSeq

        RaftConfiguration(
            RaftHost(nodeId, address), 
            peersForNode, 
            "FakeLogLocation",
            getTimerTickGranularity nodeId,
            getElectionTimeout nodeId,
            getRetryInterval nodeId)

    member __.Initialize() =
        [for nodeId in [1..numberOfNodes] do yield RaftTestSystem (getConfigFor nodeId)]