namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.Operators

type RaftNode(serverFactory : Func<RaftHost, IRaftHost>, clientFactory : Func<RaftPeer, IRaftPeer>, configuration : RaftConfiguration) =

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = ()
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = ()

    let onMessage = Action<RequestMessage>(fun request ->
        match 
            !?request.AppendEntriesRequest,
            !?request.AppendEntriesResponse, 
            !?request.VoteRequest, 
            !?request.VoteResponse 
            with
                | Some appendReq, _, _, _ -> handleAppendEntriesRequest request.NodeId appendReq
                | _, Some appendRes, _, _ -> handleAppendEntriesResponse request.NodeId appendRes
                | _, _, Some voteReq, _   -> handleVoteRequest request.NodeId voteReq
                | _, _, _, Some voteRes   -> handleVoteResponse request.NodeId voteRes
                | _ -> invalidOp("invalid message!")
        ())

    member this.Server = serverFactory.Invoke(configuration.Self)

    member this.Clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node))
        |> dict

    member this.Start() =
        this.Server.Start(onMessage)
        this.Clients |> Seq.iter(fun client -> client.Value.Start())
        ()

    //member this.State : RaftStateMachine.RaftState