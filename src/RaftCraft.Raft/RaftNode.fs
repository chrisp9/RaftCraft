namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.RaftDomain
open Utils
open RaftCraft

type RaftNode
        (serverFactory : Func<RaftHost, IRaftHost>, 
         clientFactory : Func<RaftPeer, IRaftPeer>, 
         configuration : RaftConfiguration,
         electionTimer : ElectionTimerHolder) =

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = printfn "VoteRequest Received"
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = ()

    // Initially we are a follower at term 0 and we haven't voted for anyone.
    let mutable nodeState = NodeState(RaftRole.Follower, 0, Option.None)

    let eventStream = Event<DomainEvent>()

    let clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node))
        |> dict

    // Agent ensures that messages from multiple connections (threads) are handled serially.
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            eventStream.Trigger(msg)
            return! messageLoop()
        }

        messageLoop()
    )

    let onMessage (request : RequestMessage) =
        printfn "Received %s" (request.ToString())

        match request with
            | AppendEntriesRequest r  -> handleAppendEntriesRequest request.NodeId r
            | AppendEntriesResponse r -> handleAppendEntriesResponse request.NodeId r
            | VoteRequest r           -> handleVoteRequest request.NodeId r
            | VoteResponse r          -> handleVoteResponse request.NodeId r
            | _                       -> invalidOp("Unknown message") |> raise // TODO deal with this better

    let transitionToFollowerState() =
        Console.WriteLine("Transitioning to follower")
        nodeState <- NodeState(RaftRole.Candidate, nodeState.Term + 1, configuration.Self.NodeId |> Some)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))

    let transitionToCandidateState() =
        Console.WriteLine("Transitioning to candidate");

        // TODO More election management.
        nodeState <- NodeState(RaftRole.Candidate, nodeState.Term + 1, configuration.Self.NodeId |> Some)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))
    
        clients 
        |> Seq.iter(fun client -> 
            client.Value.Post(
                RequestMessage.NewVoteRequest(
                    configuration.Self.NodeId, 
                    Guid.NewGuid(), 
                    new VoteRequest(nodeState.Term, client.Key, 1, 1)))) // TODO hardcoded ints.
        ()

    let electionTimerFired() =
        match nodeState.RaftRole with
            | RaftRole.Candidate -> transitionToCandidateState()
            | RaftRole.Follower -> transitionToCandidateState()
            | RaftRole.Leader -> NotImplementedException() |> raise

    let eventStreamSubscription = 
        eventStream.Publish |> Observable.subscribe(fun msg ->
            match msg with
                | DomainEvent.Request r -> onMessage(r)
                | DomainEvent.ElectionTimerFired -> electionTimerFired())

    member __.Server = serverFactory.Invoke(configuration.Self)

    member this.Start() =
        // Important to transition to follower before starting the service to avoid race conditions.
        transitionToFollowerState()
        this.Server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        clients |> Seq.iter(fun client -> client.Value.Start())

    member __.Stop() =
        // TODO dispose server and clients nicely.
        electionTimer.Stop()
        ()