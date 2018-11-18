namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain



type RaftNode(serverFactory : Func<string, IRaftHost>, clientFactory : Func<string, IRaftPeer>, configuration : Configuration) =
    let onMessage = Action<_>(fun x -> ())

    member this.Server = serverFactory.Invoke(configuration.SelfAddress)

    member this.Clients = 
        configuration.PeerAddressses 
        |> Seq.map(fun node -> node.NodeId, clientFactory.Invoke(node.Address))
        |> dict

    member this.Start() =
        this.Server.Start(onMessage)
        this.Clients |> Seq.iter(fun client -> client.Value.Start())
        ()

    //member this.State : RaftStateMachine.RaftState