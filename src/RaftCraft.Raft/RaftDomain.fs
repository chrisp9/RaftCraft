namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftCraft.Logging
open System.Collections.Generic
open System.Xml.Linq
open System

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
    member __.Clone() : NodeState = NodeState(raftRole, term, votedFor)
    
    override __.ToString() =
        "Role: " + raftRole.ToString() + " Term: " + term.ToString() + " VotedFor: " + stringifyVotedFor()

type NodeStateHolder(configuration : RaftConfiguration, initialState : NodeState, dataStore : IPersistentDataStore) =
    let mutable nodeState : NodeState = initialState

    let getMajoritySize() =
        let nodeCount = configuration.Peers.Length + 1 // Peer nodes count + Self
        (nodeCount / 2) + 1

    // We need to keep track of who this node has received votes from in its current term so that we know 
    // when a majority has been reached
    let votesReceivedInCurrentTerm = HashSet<_>()

    let electedLeader = Event<_>()

    member __.Update(newState : NodeState) =
        let previousState = nodeState
        nodeState <- newState
        Log.Instance.Info("NodeState transition: " + newState.ToString())

        if previousState.Term <> newState.Term then votesReceivedInCurrentTerm.Clear()

        dataStore.Update(newState.Term, (newState.VotedFor |> Option.toNullable))

    member __.Current() = nodeState

    member __.Vote(node) = 
        (votesReceivedInCurrentTerm.Add node) |> ignore
        if votesReceivedInCurrentTerm.Count >= getMajoritySize() then
            electedLeader.Trigger()

    member __.LastLogIndex = dataStore.LastLogIndex
    member __.LastLogTerm = dataStore.LastLogTerm
    member __.ElectedLeader = electedLeader.Publish

type DomainEvent =
    | Request of RaftMessage
    | ElectionTimerFired
    | AppendEntriesPingFired
    | Ping of AsyncReplyChannel<DateTime>