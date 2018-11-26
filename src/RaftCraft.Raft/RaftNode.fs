namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.Operators
open RaftCraft.RaftDomain
open RaftStateMachine
open RaftCraft.ElectionTimer
open RaftCraft.Utils

type RaftNode
        (serverFactory : Func<RaftHost, IRaftHost>, 
         clientFactory : Func<RaftPeer, IRaftPeer>, 
         configuration : RaftConfiguration) =

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = printfn "VoteRequest Received"
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = ()

    // TODO Configuration for this
    let electionTimer = new ElectionTimer(int64 1)

    // Initially each node is a follower. On startup of the cluster, everyone is initially a follower until 
    // the first election is triggered as a result of not receiving AppendEntries from a leader.
    let raftState = ref (new NodeState(RaftRole.Follower, 0))

    let clients = 
        configuration.Peers 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node))
        |> dict

    let onMessage (request : RequestMessage) =
        printfn "Received %s" (request.ToString())

        match request with
            | AppendEntriesRequest r  -> handleAppendEntriesRequest request.NodeId r
            | AppendEntriesResponse r -> handleAppendEntriesResponse request.NodeId r
            | VoteRequest r           -> handleVoteRequest request.NodeId r
            | VoteResponse r          -> handleVoteResponse request.NodeId r
            | _                       -> invalidOp("Unknown message") |> raise // TODO deal with this better

    let transitionToCandidateState() =
        Console.WriteLine("Transitioning to candidate");

        // Transition to candidate and vote for self
        raftState := new NodeState(RaftRole.Candidate, raftState.Value.Term + 1)
        raftState.Value.VotedFor <- Some configuration.Self.NodeId

        electionTimer.ResetTimer()
        // TODO More election management.

        clients 
        |> Seq.iter(fun client -> 
            client.Value.Post(
                RequestMessage.NewVoteRequest(
                    configuration.Self.NodeId, 
                    Guid.NewGuid(), 
                    new VoteRequest(raftState.Value.Term, client.Key, 1, 1)))) // TODO hardcoded ints.
        ()

    let transition oldState newState =
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
                | Transition (old, upd) -> transition old upd

            return! messageLoop()
        }

        messageLoop()
    )

    let electionObservable = 
        electionTimer.Observable() 
            |> Observable.subscribe(fun args ->
                   let currentState = raftState.Value
                   agent.Post(DomainEvent.Transition(currentState, RaftRole.Candidate)))

    member __.Server = serverFactory.Invoke(configuration.Self)

    member this.Start() =
        this.Server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        clients |> Seq.iter(fun client -> client.Value.Start())
        electionTimer.Start()
   
    member __.Stop() =
        // TODO dispose server and clients nicely.
        electionObservable.Dispose()