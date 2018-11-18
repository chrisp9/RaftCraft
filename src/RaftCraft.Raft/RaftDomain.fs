namespace RaftCraft.RaftDomain

open RaftCraft.Domain
open RaftCraft.Interfaces

type DomainEvent =
    | Request of RequestMessage
    | TermExpired
    | ElectionTimeout of IRaftPeer