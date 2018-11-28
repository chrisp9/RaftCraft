namespace RaftCraft.RaftDomain

open RaftCraft.Domain

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(raftRole, term : int, votedFor) =
    member __.RaftRole : RaftRole = raftRole
    member __.Term = term
    member __.VotedFor : Option<int> = votedFor


type NodeStateHolder(initialState : NodeState) =
    let mutable nodeState : NodeState = initialState

    member __.Update(newState : NodeState) =
        nodeState <- newState

    member __.Current() = nodeState

type DomainEvent =
    | Request of RaftMessage
    | ElectionTimerFired

[<Struct>]
type TimerTick = { Granularity : int64; CurrentTick : int64 }