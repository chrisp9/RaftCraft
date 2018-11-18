module RaftStateMachine

type RaftState =
    | Leader
    | Follower
    | Candidate