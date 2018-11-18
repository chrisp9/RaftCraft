namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain

type RaftNode(serverFactory : Func<string, IServer>, clientFactory : Func<string, IClient>, configuration : Configuration) =
    member this.Server = serverFactory.Invoke(configuration.SelfAddress)

    member this.Clients = 
        configuration.PeerAddressses 
        |> Seq.map(fun address -> address, clientFactory.Invoke(address))
        |> dict

    member this.Start() =
        this.Server.Start()
        this.Clients |> Seq.iter(fun client -> client.Value.Start())
        ()

    //member this.State : RaftStateMachine.RaftState