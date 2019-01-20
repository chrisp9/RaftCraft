namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain
open RaftCraft.RaftDomain
open Utils
open RaftCraft
open RaftCraft.Logging
open System.Xml.Linq
open System.Xml.Linq

type RaftNode
        (serverFactory : RaftHost -> IRaftHost,
         configuration : RaftConfiguration,
         electionTimer : ElectionTimerHolder,
         peerSupervisor : NodeStateHolder -> PeerSupervisor,
         nodeStateHolderFactory : NodeState -> NodeStateHolder) =

    // Initially we are a follower at term 0 and we haven't voted for anyone.    
    let nodeState = NodeState(RaftRole.Follower, 0, Option.None) |> nodeStateHolderFactory
    let peerSupervisor = peerSupervisor nodeState

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

    let transitionToLeaderState() =
        Log.Instance.Info("Transitioning to leader")
        electionTimer.Stop()

        nodeState.Update <| NodeState(RaftRole.Leader, nodeState.Current().Term + 1, None)

    
    let transitionToFollowerState term =
        Log.Instance.Info("Transitioning to follower")

        nodeState.Update <| NodeState(RaftRole.Follower, term, None)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))

    let transitionToCandidateState() =
        Log.Instance.Info("Transitioning to candidate");

        nodeState.Update <| NodeState(RaftRole.Candidate, nodeState.Current().Term + 1, Some configuration.Self.NodeId)
        electionTimer.Start(fun _ -> agent.Post(DomainEvent.ElectionTimerFired))
        peerSupervisor.RequestVote()

    let handleVoteRequest (msg : RaftMessage) (r : VoteRequest) = 
        let currentState = nodeState.Current()

        let newTerm = Math.Max(nodeState.Current().Term, r.Term)

        // If we see a newer term, we should ensure that we become follower and vote for the node with the newer term, otherwise
        // we should only vote if the request term matches the current term and request log indexes and terms are at least as
        // up to date as ours -- and we haven't already voted for a different node in the current term.
        if r.Term > nodeState.Current().Term then
            electionTimer.Reset()
            transitionToFollowerState r.Term
        else
            let candidateCheck =
                match currentState.VotedFor with
                | Some v -> if v = msg.SourceNodeId then true else false
                | None -> true

            let isSuccess = 
                if (r.Term = currentState.Term && r.LastLogIndex >= nodeState.LastLogIndex && r.LastLogTerm >= nodeState.LastLogTerm && candidateCheck) 
                then true 
                else false

            let votedFor = 
               match isSuccess with
                    | true -> Some msg.SourceNodeId
                    | false -> None

            if isSuccess then 
                Log.Instance.Info("Successful response")
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
    
    // TODO Refactor so that we only wire up subscriptions relevant to the role we're currently in. Realistically this means the "Role" Union
    // holds all subscriptions needed for that role.
    let electionTimerFired() =
        match nodeState.Current().RaftRole with
            | RaftRole.Candidate -> transitionToCandidateState() // Candidate -> Candidate = split vote
            | RaftRole.Follower -> transitionToCandidateState()  // Follower -> Candidate = leader is assumed down
            | RaftRole.Leader -> Log.Instance.Warn("Election timer fired whilst leader. Ignoring")

    let eventStreamSubscription = 
        eventStream.Publish |> Observable.subscribe(fun msg ->
            match msg with
                | DomainEvent.Request r -> onMessage(r)
                | DomainEvent.ElectionTimerFired -> electionTimerFired()
                | DomainEvent.AppendEntriesPingFired -> ()) // TODO handle ping fired.

    let leaderElectionSubscription = 
        nodeState.ElectedLeader |> Observable.subscribe(fun() -> 
            transitionToLeaderState())

    member __.Server = server

    member __.Start() =
        // Important to transition to follower before starting the server to avoid race conditions.
        transitionToFollowerState(nodeState.Current().Term + 1)
        server.Start (fun msg -> agent.Post(DomainEvent.Request(msg)))
        peerSupervisor.Start()

    member __.Stop() =
        // TODO dispose server and clients nicely.
        electionTimer.Stop()
        eventStreamSubscription.Dispose()
        ()

    member __.State() = nodeState.Current().Clone()