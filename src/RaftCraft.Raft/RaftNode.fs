namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.RaftDomain
open Utils
open RaftCraft
open RaftCraft.Logging
open Subscription

type RaftNode
        (serverFactory : RaftHost -> IRaftHost,
         configuration : RaftConfiguration,
         electionTimer : ElectionTimerHolder,
         peerSupervisor : NodeStateHolder -> PeerSupervisor,
         nodeStateHolderFactory : NodeState -> NodeStateHolder) =
    
    let nodeState = NodeState(RaftRole.Follower, 0, Option.None) |> nodeStateHolderFactory
    let peerSupervisor = peerSupervisor nodeState

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

    let handleVoteRequest (msg : RaftMessage) (r : VoteRequest) = 
        let currentState = nodeState.Current()
   
        let candidateCheck =
            match currentState.VotedFor with
            | Some v -> if v = r.CandidateId then true else false
            | None -> true

        let isSuccess = 
            if (r.Term >= currentState.Term && r.LastLogIndex >= nodeState.LastLogIndex && r.LastLogTerm >= nodeState.LastLogTerm && candidateCheck) 
            then true 
            else false

        let newTerm = Math.Max(nodeState.Current().Term, r.Term)

        let votedFor = 
           match isSuccess with
                | true -> Some r.CandidateId
                | false -> None

        if isSuccess then 
            electionTimer.Reset()

        nodeState.Update <| NodeState(nodeState.Current().RaftRole, newTerm, votedFor)
        peerSupervisor.RespondToVoteRequest msg.RequestId msg.SourceNodeId isSuccess

    let onMessage (msg : RaftMessage) =
        Log.Instance.Info("Received " + msg.ToString())

        match msg with
            | VoteRequest r -> handleVoteRequest msg r
            | VoteResponse r -> peerSupervisor.HandleVoteResponse msg.RequestId msg.SourceNodeId r.VoteGranted
            | _ -> invalidOp("Unknown message") |> raise // TODO deal with this better
        ()
    
    let transitionToFollowerState() =
        Log.Instance.Info("Transitioning to follower")

        nodeState.Update <| NodeState(RaftRole.Candidate, nodeState.Current().Term + 1, None)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))

    let transitionToCandidateState() =
        Log.Instance.Info("Transitioning to candidate");

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

    member __.Start() =
        // Important to transition to follower before starting the server to avoid race conditions.
        transitionToFollowerState()
        server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        peerSupervisor.Start()

    member __.Stop() =
        // TODO dispose server and clients nicely.
        electionTimer.Stop()
        eventStreamSubscription.Dispose()
        ()