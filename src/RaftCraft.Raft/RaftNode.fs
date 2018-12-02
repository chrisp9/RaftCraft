namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.RaftDomain
open Utils
open RaftCraft

type RaftNode
        (serverFactory : RaftHost -> IRaftHost,
         configuration : RaftConfiguration,
         electionTimer : ElectionTimerHolder,
         peerSupervisor : NodeStateHolder -> PeerSupervisor,
         nodeStateHolderFactory : NodeState -> NodeStateHolder) =
    
    let nodeState = NodeState(RaftRole.Follower, 0, Option.None) |> nodeStateHolderFactory

    let peerSupervisor = peerSupervisor nodeState

    let handleAppendEntriesRequest id appendEntriesRequest = ()
    let handleVoteRequest id voteRequest = peerSupervisor.RespondToVoteRequest voteRequest
    let handleAppendEntriesResponse id appendEntriesResponse = ()
    let handleVoteResponse id voteResponse = peerSupervisor.HandleVoteResponse voteResponse

    // Initially we are a follower at term 0 and we haven't voted for anyone.


    let eventStream = Event<DomainEvent>()
    let server = serverFactory configuration.Self

    // Agent ensures that messages from multiple connections (threads) are handled serially.
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            eventStream.Trigger(msg)
            return! messageLoop()
        }

        messageLoop()
    )

    let onMessage (msg : RaftMessage) =
        printfn "Received %s" (msg.ToString())

        match msg with
            | VoteRequest r           -> 
                let currentState = nodeState.Current()
   
                let candidateCheck =
                    match currentState.VotedFor with
                    | Some v -> if v = r.CandidateId then true else false
                    | None -> true

                let isSuccess = 
                    if (r.Term >= currentState.Term  && r.LastLogIndex >= nodeState.LastLogIndex && r.LastLogTerm >= nodeState.LastLogTerm && candidateCheck) 
                    then true 
                    else false

                nodeState.Update <| NodeState(nodeState.Current().RaftRole, nodeState.Current().Term, Some r.CandidateId)
                peerSupervisor.RespondToVoteRequest msg.RequestId msg.SourceNodeId isSuccess
            | VoteResponse r          -> peerSupervisor.HandleVoteResponse msg
            | _                       -> invalidOp("Unknown message") |> raise // TODO deal with this better
        ()
    
    let transitionToFollowerState() =
        Console.WriteLine("Transitioning to follower")

        nodeState.Update <| NodeState(RaftRole.Candidate, nodeState.Current().Term + 1, None)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))

    let transitionToCandidateState() =
        Console.WriteLine("Transitioning to candidate");

        nodeState.Update <| NodeState(RaftRole.Candidate, nodeState.Current().Term + 1, Some configuration.Self.NodeId)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))
        peerSupervisor.RequestVote()

    let electionTimerFired() =
        match nodeState.Current().RaftRole with
            | RaftRole.Candidate -> transitionToCandidateState()
            | RaftRole.Follower -> transitionToCandidateState()
            | RaftRole.Leader -> NotImplementedException() |> raise

    let eventStreamSubscription = 
        eventStream.Publish |> Observable.subscribe(fun msg ->
            match msg with
                | DomainEvent.Request r -> onMessage(r)
                | DomainEvent.ElectionTimerFired -> electionTimerFired())

    member __.Server = server

    member this.Start() =
        // Important to transition to follower before starting the server to avoid race conditions.
        transitionToFollowerState()
        server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        peerSupervisor.Start()

    member __.Stop() =
        // TODO dispose server and clients nicely.
        electionTimer.Stop()
        eventStreamSubscription.Dispose()
        ()