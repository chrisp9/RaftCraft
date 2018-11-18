namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces
open RaftCraft.Domain

type RaftNode(serverFactory : Func<IServer>, clientFactory : Func<string, IClient>, configuration : Configuration) =
    member this.Server = serverFactory.Invoke()
    member val Clients = dict[] with get, set

    member this.Start() =
        this.Server.Start()
 
        this.Clients <- 
            configuration.PeerAddressses 
            |> Seq.map(fun address -> address, clientFactory.Invoke(address))
            |> dict
        ()

    //member this.State : RaftStateMachine.RaftState