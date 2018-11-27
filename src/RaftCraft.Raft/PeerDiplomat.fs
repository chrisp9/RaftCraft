namespace RaftCraft.Raft

open RaftCraft.Interfaces
open RaftCraft.Domain
open System
open System.Collections.Generic
open Utils
open RaftTimer
open RaftCraft.RaftDomain

type RetryCount = int
type ExpiryTick = int64

// Holds entries for a fixed amount of time.
type Pipeline(retryIntervalMs : int) =
    let requestsById = Dictionary<Guid, RequestMessage>()
    let requestsByTick = SortedDictionary<ExpiryTick, HashSet<Guid>>()

    // We expect that the pool size will eventually remain constant, so we benefit from pooling. We only expect 
    // entries for N time slots. At each tick, we retry any items scheduled for that tick and return this HashSet
    // to the pool.
    let pool = HashSetPool()

    member __.Add(message : RequestMessage, currentTick : TimerTick) =

        let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))

        let mutable success, set = requestsByTick.TryGetValue(expiryTick)
        if (not success) then 
            set <- pool.Borrow()
            requestsByTick.[expiryTick] <- set
        
        set.Add(message.RequestId)


// A diplomat is responsible for negotiating with a particular Peer. How negotation proceeds depends on
// the political landscape. For example, if a peer does not respond to certain requests within a
// sensible time, we need to retry that request.
type PeerDiplomat(peer : IRaftPeer) =
    let inFlightAppendEntries = Queue<RequestMessage>()

    member __.Post(message : RequestMessage) =
        // TODO:
        match message with
            | AppendEntriesRequest req -> peer.Post(message)
            | VoteRequest req -> peer.Post(message)
            | _ -> ()

// PeerSupervisor acts as a router layer in deciding which Peer(s) need to be contacted for a given request.
// It exposes an Observable to let the host Node know when consensus has been reached.
// Each new Term, it's temporary state needs to be reset.
type PeerSupervisor(configuration : RaftConfiguration, nodeState : NodeStateHolder, clientFactory : RaftPeer -> IRaftPeer) =
    let clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory node)
        |> dict

    let broadcastToAll create =
        clients
        |> Seq.iter(fun client -> client.Value.Post (create client.Key))

    let newVoteRequest key =
        RequestMessage.NewVoteRequest(
            configuration.Self.NodeId,
            Guid.NewGuid(),
            new VoteRequest(nodeState.Current().Term, key, 1, 1))

    member __.VoteRequest() = 
        newVoteRequest |> broadcastToAll

    member __.Start() =
        clients |> Seq.iter(fun client -> client.Value.Start())