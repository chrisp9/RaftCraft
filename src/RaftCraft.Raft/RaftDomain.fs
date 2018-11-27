namespace RaftCraft.RaftDomain

open RaftCraft.Domain

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
    | ElectionTimerFired