namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open Subscription

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(raftRole, term : int, votedFor) =
    member __.RaftRole : RaftRole = raftRole
    member __.Term = term
    member val VotedFor : Option<int> = votedFor with get, set

type DomainEvent =
    | Request of RequestMessage

    // Transition(oldState -> newState) We care about old state, since some state transitions will be considered invalid,
    // as they are placed on queues. Sometimes we may want to disregard a transition of state has changed whilst on the 
    // RaftNode agent queue.
    | Transition of RaftRole