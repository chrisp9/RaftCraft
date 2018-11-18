namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftStateMachine

type DomainEvent =
    | Request of RequestMessage

    // Transition(oldState -> newState) We care about old state, since some state transitions will be considered invalid,
    // as they are placed on queues. Sometimes we may want to disregard a transition of state has changed whilst on the 
    // RaftNode agent queue.
    | Transition of NodeState * RaftRole