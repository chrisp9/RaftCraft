namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftCraft.Logging
open System.Collections.Generic

type RaftRole =
    | Follower
    | Candidate
    | Leader

type NodeState(configuration : RaftConfiguration, raftRole, term : int, votedFor) =
    let getMajoritySize =
        let nodeCount = configuration.Peers.Length + 1 // Peer nodes count + Self
        (nodeCount / 2) + 1

    let votes = HashSet<_>()

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

    // We need to keep track of who this node has voted for in its current term so that we know 
    // when a majority has been reached.
    let votedForInCurrentTerm = HashSet<_>()

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