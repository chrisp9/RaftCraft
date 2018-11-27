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

        // We translate from Func<_> to F#Func because this code is called from C# and want to keep
        // the boundary clean.
        let translatedServerFactory = fun v -> serverFactory.Invoke(v)
        let translatedClientFactory = fun v -> clientFactory.Invoke(v)

        let peerSupervisorFactory = fun v -> new PeerSupervisor(configuration, v, translatedClientFactory)

        let raftNode =  RaftNode(translatedServerFactory, configuration, electionTimerHolder, peerSupervisorFactory)
        timerHolder.Start()

        raftNode