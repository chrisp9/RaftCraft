namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftCraft.Logging

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(raftRole, term : int, votedFor) =
    let stringifyVotedFor() = 
        match votedFor with
            | Some v -> v.ToString()
            | None -> "None"
    
    member __.RaftRole : RaftRole = raftRole
    member __.Term = term
    member __.VotedFor : Option<int> = votedFor

    override __.ToString() =
        "Role: " + raftRole.ToString() + " Term: " + term.ToString() + " VotedFor: " + stringifyVotedFor()

type NodeStateHolder(initialState : NodeState, dataStore : IPersistentDataStore) =
    let mutable nodeState : NodeState = initialState

    member __.Update(newState : NodeState) =
        nodeState <- newState
        Log.Instance.Info("NodeState transition: " + newState.ToString())

        dataStore.Update(newState.Term, (newState.VotedFor |> Option.toNullable))

    member __.Current() = nodeState

    member __.LastLogIndex = dataStore.LastLogIndex
    member __.LastLogTerm = dataStore.LastLogTerm

type DomainEvent =
    | Request of RaftMessage
    | ElectionTimerFired

[<Struct>]
type TimerTick = { Granularity : int64; CurrentTick : int64 }