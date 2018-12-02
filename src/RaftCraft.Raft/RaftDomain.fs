namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(raftRole, term : int, votedFor) =
    member __.RaftRole : RaftRole = raftRole
    member __.Term = term
    member __.VotedFor : Option<int> = votedFor

type NodeStateHolder(initialState : NodeState, dataStore : IPersistentDataStore) =
    let mutable nodeState : NodeState = initialState

    member __.Update(newState : NodeState) =
        nodeState <- newState
        dataStore.Update(newState.Term, (newState.VotedFor |> Option.toNullable))

    member __.Current() = nodeState

    member __.LastLogIndex = dataStore.LastLogIndex
    member __.LastLogTerm = dataStore.LastLogTerm

type DomainEvent =
    | Request of RaftMessage
    | ElectionTimerFired

[<Struct>]
type TimerTick = { Granularity : int64; CurrentTick : int64 }