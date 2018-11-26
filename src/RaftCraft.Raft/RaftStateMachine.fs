namespace RaftCraft.Raft

open RaftCraft.Domain
open RaftCraft.RaftDomain
open RaftCraft
open Subscription

type RaftStateMachine(initialState : NodeState, configuration : RaftConfiguration, electionTimerFactory : unit -> ElectionTimer) =
    let mutable state = initialState
    let mutable electionTimer : Subscription<Election> option = Option.None

    let eventStream = Event<DomainEvent>()

    let update newState =
        state <- newState
        state

    let initializeElectionTimer() =
        match electionTimer with
            | Some v -> 
                v.Dispose()
                electionTimer <- Option.None
            | _ -> ()

        electionTimer <- electionTimerFactory().Subscribe(fun _ -> 
            eventStream.Trigger(DomainEvent.Transition(RaftRole.Candidate))) |> Some
    
    member __.EventStream = eventStream.Publish

    member __.BecomeFollower() : NodeState =
         initializeElectionTimer()

         new NodeState(
             RaftRole.Follower, 
             state.Term + 1, 
             Option.None) |> update

    member __.BecomeCandidate() : NodeState =
         initializeElectionTimer()

         new NodeState(
             RaftRole.Candidate, 
             state.Term + 1, 
             configuration.Self.NodeId |> Some) |> update