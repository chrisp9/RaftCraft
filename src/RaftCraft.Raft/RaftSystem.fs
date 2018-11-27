module RaftSystem

open System
open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftTimer
open RaftCraft
open RaftCraft.Raft

type RaftSystem() = 
    static member Create
        (serverFactory : Func<RaftHost, IRaftHost>, 
         clientFactory : Func<RaftPeer, IRaftPeer>, 
         configuration : RaftConfiguration) =
    
        let timerHolder = GlobalTimerHolder(Func<_,_>(fun v -> new GlobalTimer(v)), int64 50)

        let electionTimerFactory = fun() -> ElectionTimer(timerHolder, int64 1000)
        let electionTimerHolder = ElectionTimerHolder(electionTimerFactory)

        let raftNode =  RaftNode(serverFactory, clientFactory, configuration, electionTimerHolder)
        timerHolder.Start()

        raftNode