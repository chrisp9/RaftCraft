namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.Operators
open RaftCraft.RaftDomain
open RaftStateMachine

type RaftNode(serverFactory : Func<RaftHost, IRaftHost>, clientFactory : Func<RaftPeer, IRaftPeer>, configuration : RaftConfiguration) =

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = ()
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = ()

    let mut raftState = RaftState.Follower 0

    let onMessage (request : RequestMessage) =
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

    // Agent ensures that messages from multiple connections (threads) are handled serially. Ensures thread safety.
    // It also means we need to be careful for example, if ElectionTimeout happens but gets queued behind an incoming
    // Request from the node, then we did eventually receive a message from the node so don't want to start an election.
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()

            match msg with
                | Request req -> onMessage(req)
                | TermExpired -> ()
                | ElectionTimeout node -> ()

            return! messageLoop()
        }

        messageLoop()
    )

    member this.Server = serverFactory.Invoke(configuration.Self)

    member this.Clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node))
        |> dict

    member this.Start() =
        this.Server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        this.Clients |> Seq.iter(fun client -> client.Value.Start())
        ()

    //member this.State : RaftStateMachine.RaftState