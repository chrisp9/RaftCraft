namespace RaftCraft.Raft

open RaftCraft.Interfaces
open RaftCraft.Domain
open Operators
open Utils

// A diplomat is responsible for negotiating with a particular Peer. How negotation proceeds depends on
// the political landscape. For example, if a peer does not respond to certain requests within a
// sensible time, we need to retry that request.
type PeerDiplomat(peer : IRaftPeer) =
    

    member __.Post(message : RequestMessage) =
        match message with
            | AppendEntriesRequest req -> ()

// PeerSupervisor is responsible for managing the PeerDiplomats to ensure they route messages to 
// the correct peers Peers. It delegates the handling of messages to the Peers it manages.
type PeerSupervisor(configuration : RaftConfiguration) = class end
    