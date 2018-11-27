﻿namespace RaftCraft.Raft

open RaftCraft.Interfaces
open RaftCraft.Domain
open System
open System.Collections.Generic
open Utils
open RaftTimer
open RaftCraft.RaftDomain

type RetryCount = int
type ExpiryTick = int64

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