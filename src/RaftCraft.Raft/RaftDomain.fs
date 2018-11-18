namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftStateMachine

type DomainEvent =
    | Request of RequestMessage
    | Transition of RaftState * RaftState