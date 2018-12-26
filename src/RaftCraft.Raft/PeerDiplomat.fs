namespace RaftCraft.Raft

open RaftCraft.Interfaces
open RaftCraft.Domain
open System
open Utils
open RaftCraft.RaftDomain
open RaftTimer
open System.Collections.Generic

type RetryCount = int
type ExpiryTick = int64

// A diplomat is responsible for negotiating with a particular Peer. How negotation proceeds depends on
// the political landscape. For example, if a peer does not respond to certain requests within a
// sensible time, we need to retry that request.
type PeerDiplomat(peer : IRaftPeer, nodeState : NodeStateHolder, retryIntervalMs : int, timer : GlobalTimerHolder) =
    let retryPipeline = Pipeline(retryIntervalMs)

    let track message =
        match message with
            | AppendEntriesRequest _ | VoteRequest _   -> retryPipeline.Add(message, timer.CurrentTick)
            | VoteResponse _ | AppendEntriesResponse _ -> retryPipeline.Remove(message.RequestId) |> ignore
            | _ -> ()

    let checkExpiries (tick : TimerTick) =
            retryPipeline.Expiry (tick) nodeState (fun msg -> peer.Post(msg))

    // TODO Consider whether it is best to check for expiry every clock tick? It's good in some ways but bad in others.
    let subscription = timer.Observable().Subscribe(checkExpiries)

    member __.HandleResponse(request : Guid) =
        retryPipeline.Remove(request) |> ignore

    member __.Post(message : RaftMessage) =
        track message
        peer.Post(message)

    member __.Start() =
       peer.Start()
    
    // TODO call me
    member __.Stop() =
        subscription.Dispose()
    
// PeerSupervisor acts as a router layer in deciding which Peer(s) need to be contacted for a given request.
// It exposes an Observable to let the host Node know when consensus has been reached.
// Each new Term, it's temporary state needs to be reset.
type PeerSupervisor(configuration : RaftConfiguration, nodeState : NodeStateHolder, clientFactory : RaftPeer -> NodeStateHolder -> PeerDiplomat) =
    let clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory node nodeState)
        |> dict
    
    let nodesWithConsensus = HashSet<int>()

    let broadcastToAll create =
        clients
        |> Seq.iter(fun client -> client.Value.Post (create client.Key))

    let newVoteRequest key =
        RaftMessage.NewVoteRequest(
            configuration.Self.NodeId,
            Guid.NewGuid(),
            new VoteRequest(nodeState.Current().Term, key, nodeState.LastLogIndex, nodeState.LastLogTerm))

    let electionConsensusReached = Event<_>()

    member __.RequestVote() =
        newVoteRequest |> broadcastToAll

    member __.RespondToVoteRequest (requestId : Guid) (sourceNodeId : int) (isSuccess : bool) =
        let response = RaftMessage.NewVoteResponse(configuration.Self.NodeId, requestId, new VoteResponse(nodeState.Current().Term, isSuccess))
        clients.[sourceNodeId].Post(response)

    member __.HandleVoteResponse(request : Guid) (sourceNodeId : int) (isSuccess : bool) =
        clients.[sourceNodeId].HandleResponse(request)

        if(isSuccess) then
            nodesWithConsensus.Add(sourceNodeId) |> ignore
            if nodesWithConsensus.Count > (configuration.Peers.Length + 1) / 2 then
                electionConsensusReached.Trigger()

    member __.Start() =
        clients |> Seq.iter(fun client -> client.Value.Start())