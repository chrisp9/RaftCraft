namespace RaftCraft.Raft

open System
open RaftCraft.Interfaces

type RaftNode(serverFactory : Func<IServer>, clientFactory : Func<IClient>) =
    member this.Server = serverFactory.Invoke()
    member this.Start() = this.Server.Start()