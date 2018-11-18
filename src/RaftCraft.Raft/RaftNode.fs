namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.Operators

type RaftNode(serverFactory : Func<RaftHost, IRaftHost>, clientFactory : Func<RaftPeer, IRaftPeer>, configuration : RaftConfiguration) =

    let handleAppendEntries id appendEntries = ()
    let handleVote id vote = ()

    let onMessage = Action<RequestMessage>(fun request ->
        match !?request.AppendEntriesRequest, !?request.VoteRequest with
            | Some append, _ -> handleAppendEntries request.NodeId append
            | _, Some vote   -> handleVote request.NodeId vote
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