namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.RaftDomain
open RaftCraft.Utils
open RaftTimer
open RaftCraft
open Subscription

type RaftNode
        (serverFactory : Func<RaftHost, IRaftHost>, 
         clientFactory : Func<RaftPeer, IRaftPeer>, 
         configuration : RaftConfiguration,
         stateMachineFactory : NodeState -> RaftStateMachine) =

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = printfn "VoteRequest Received"
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = ()

    // Initially we are a follower, with term 0 and haven't yet voted for anyone
    let stateMachine = stateMachineFactory(new NodeState(RaftRole.Follower, 0, Option.None))

    let onMessage (request : RequestMessage) =
        printfn "Received %s" (request.ToString())

        match request with
            | AppendEntriesRequest r  -> handleAppendEntriesRequest request.NodeId r
            | AppendEntriesResponse r -> handleAppendEntriesResponse request.NodeId r
            | VoteRequest r           -> handleVoteRequest request.NodeId r
            | VoteResponse r          -> handleVoteResponse request.NodeId r
            | _                       -> invalidOp("Unknown message") |> raise // TODO deal with this better

    let clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node))
        |> dict

    let transitionToCandidateState() =
        Console.WriteLine("Transitioning to candidate");

        // TODO More election management.
        let newState = stateMachine.BecomeCandidate()

        clients 
        |> Seq.iter(fun client -> 
            client.Value.Post(
                RequestMessage.NewVoteRequest(
                    configuration.Self.NodeId, 
                    Guid.NewGuid(), 
                    new VoteRequest(newState.Term, client.Key, 1, 1)))) // TODO hardcoded ints.
        ()

    let transition newState =
        match newState with
            | RaftRole.Candidate -> transitionToCandidateState()
            | RaftRole.Follower -> NotImplementedException() |> raise
            | RaftRole.Leader -> NotImplementedException() |> raise

    // Agent ensures that messages from multiple connections (threads) are handled serially. Ensures thread safety.
    // It also means we need to be careful for example, if ElectionTimeout happens but gets queued behind an incoming
    // Request from the node, then we did eventually receive a message from the node so don't want to start an election.
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()

            match msg with
                | Request req -> onMessage(req)
                | Transition (upd) -> transition upd

            return! messageLoop()
        }

        messageLoop()
    )

    member this.Post(role) =
        agent.Post(DomainEvent.Transition(role))

    member __.Server = serverFactory.Invoke(configuration.Self)

    member this.Start() =
        this.Server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        clients |> Seq.iter(fun client -> client.Value.Start())

    member __.Stop() =
        // TODO dispose server and clients nicely.
        ()

and RaftStateMachine(node : RaftNode, initialState : NodeState, configuration : RaftConfiguration, electionTimerFactory : unit -> ElectionTimer) =
    let mutable state = initialState
    let mutable electionTimer : Subscription<Election> option = Option.None

    let initializeElectionTimer() =
        match electionTimer with
            | Some v -> 
                v.Dispose()
                electionTimer <- Option.None
            | _ -> ()

        electionTimer <- electionTimerFactory().Subscribe(fun _ -> 
            node.Post(RaftRole.Candidate)) |> Some


    member __.BecomeFollower() : NodeState =
         initializeElectionTimer()

         state <- new NodeState(
             RaftRole.Follower, 
             state.Term + 1, 
             Option.None)

         state

    member __.BecomeCandidate() : NodeState =
         initializeElectionTimer()

         state <- new NodeState(
             RaftRole.Candidate, 
             state.Term + 1, 
             Option.None)
        
         electionTimer <- electionTimerFactory().Subscribe(fun _ -> 
            node.Post(RaftRole.Follower)) |> Some

         state