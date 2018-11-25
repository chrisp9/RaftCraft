module RaftStateMachine

type Term = int

type NodeState(raftRole, term : int) =
    member __.RaftRole = raftRole
    member __.Term = term
    member val VotedFor : int option = Option.None with get, set

type RaftRole =
    | Follower
    | Leader
    | Candidate