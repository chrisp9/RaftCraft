namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open System.Xml.Linq

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(raftRole, term : int, votedFor) =
    member __.RaftRole : RaftRole = raftRole
    member __.Term = term
    member val VotedFor : Option<int> = votedFor with get, set

type NodeStateHolder(initialState : NodeState) =
    let mutable nodeState : NodeState = initialState

    member __.Update(newState : NodeState) =
        nodeState <- newState

    member __.Current() = nodeState

type DomainEvent =
    | Request of RequestMessage
    | ElectionTimerFired
