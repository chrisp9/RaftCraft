module RaftSystem

open System
open RaftCraft.Domain
open RaftCraft.Interfaces
open RaftTimer
open RaftCraft
open RaftCraft.Raft
open RaftCraft.RaftDomain

type RaftSystem() = 
    static member Create
        (serverFactory : Func<RaftHost, IRaftHost>, 
         clientFactory : Func<RaftPeer, IRaftPeer>, 
         persistentDataStore : IPersistentDataStore,
         configuration : RaftConfiguration) =
    
        // TODO Make the hardcoded values here configurable.
        let globalTimerFactory = fun v -> new GlobalTimer(v)
        let timerHolder = GlobalTimerHolder(globalTimerFactory, int64 50)

        let electionTimerFactory = fun() -> ElectionTimer(timerHolder, int64 1000)
        let electionTimerHolder = ElectionTimerHolder(electionTimerFactory)

        // We translate from Func<_> to F#Func because this code is called from C# and want to keep
        // the boundary clean.
        let translatedServerFactory = serverFactory.Invoke
        let translatedClientFactory = clientFactory.Invoke

        let peerSupervisorFactory = fun v -> new PeerSupervisor(configuration, v, fun v -> new PeerDiplomat(translatedClientFactory(v), 200, timerHolder))
        let nodeStateHolderFactory = fun v -> new NodeStateHolder(v, persistentDataStore)

        let raftNode =  RaftNode(translatedServerFactory, configuration, electionTimerHolder, peerSupervisorFactory, nodeStateHolderFactory)
        timerHolder.Start()

        raftNode