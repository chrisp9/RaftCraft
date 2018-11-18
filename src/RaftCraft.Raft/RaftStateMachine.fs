module RaftStateMachine

type Term = int

type RaftState =
    | Follower of Term
    | Leader of Term
    | Candidate of Term

    member this.GetTerm() =
        match this with
            | Follower term -> term
            | Leader term -> term
            | Candidate term -> term