module RaftStateMachine

type Term = int

type NodeState(raftRole, term) =
    member __.RaftRole = raftRole
    member __.Term = term

    member __.AdvanceTo(target : NodeState) =
        NodeState(target.RaftRole, (target.Term + 1))

type RaftRole =
    | Follower
    | Leader
    | Candidate