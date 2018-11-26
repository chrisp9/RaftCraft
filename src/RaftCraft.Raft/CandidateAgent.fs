module CandidateAgent

open RaftCraft

type CandidateAgent(electionTimerFactory : unit -> ElectionTimer) = 
    let electionTimer = electionTimerFactory()

    member __.Start() =
        electionTimer.Subscribe(fun tick -> 
            electionTimer.Reset()

            )