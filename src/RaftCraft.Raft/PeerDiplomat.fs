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

    // We expect that the amortised number of ExpiryTicks will be O(1), so we benefit from pooling. We only expect 
    // entries for N time slots. At each tick, we retry any items scheduled for that tick and return this HashSet
    // to the pool.
    let pool = HashSetPool()

    let getOrAdd(dict : IDictionary<'a, 'b>, key : 'a, valueFactory : unit -> 'b) =
        let mutable success, value = dict.TryGetValue(key)
        if (not success) then
            value <- valueFactory()
            dict.[key] <- value
        value
    
    // TODO Although the Raft specification says that we should retry indefinitely, I'm not convnced that this is a good
    // idea. We can't just store these items in memory indefinitely, since under sufficient load we would eventually 
    // hit performance problems. A better approach may be to retry the action for a given number of times, then completely 
    // back off, clear the state and in future only send the periodic empty AppendEntriesRequests until we hear back a 
    // successful response. The main reason for the retries is to attempt to avoid packet loss from causing election timeouts. 
    // Importantly, the Raft algorithm makes no assumptions about the underlying protocol or message ordering.
    // This implementation doesn't even guarantee message ordering. Incoming messages are optimistically sent to peers, but we
    // may have many prior messages in the retry collection. We have to be optimistic - assume the delayed messages will
    // eventually received by the peer, and let the algorithm handle any state inconsistences as a result of message reordering.
    // Enforcing only one in-flight message per peer would be too slow.

    // For now though, we just retry indefinitely.
    member __.Add(message : RequestMessage, currentTick : TimerTick) =

        let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))

        let expiriesForThisTick = getOrAdd(requestsByTick, expiryTick, fun() -> pool.Borrow())

        expiriesForThisTick.Add(message.RequestId) |> ignore

        requestsById.[message.RequestId] <- message

    

// A diplomat is responsible for negotiating with a particular Peer. How negotation proceeds depends on
// the political landscape. For example, if a peer does not respond to certain requests within a
// sensible time, we need to retry that request.
type PeerDiplomat(peer : IRaftPeer) =
    let retryPipeline = Pipeline()

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