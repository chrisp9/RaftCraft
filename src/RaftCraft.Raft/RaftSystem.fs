﻿module RaftSystem

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
         globalTimerFactory : Func<int64, IGlobalTimer>,
         configuration : RaftConfiguration) =
    
        let globalTimerFactoryFun = fun v -> globalTimerFactory.Invoke(v)
        
        let timerHolder = GlobalTimerHolder(globalTimerFactoryFun, int64 configuration.GlobalTimerTickInterval)

        let electionTimerFactory = fun() -> ElectionTimer(timerHolder, int64 configuration.ElectionTimeout)
        let electionTimerHolder = ElectionTimerHolder(electionTimerFactory)

        // We translate from Func<_> to F#Func because this code is called from C# and want to keep
        // the boundary clean.
        let translatedServerFactory = serverFactory.Invoke
        let translatedClientFactory = clientFactory.Invoke

        let peerSupervisorFactory = fun v -> new PeerSupervisor(configuration, v, fun v -> fun ns -> new PeerDiplomat(translatedClientFactory(v), ns, configuration.RequestPipelineRetryInterval, timerHolder))
        let nodeStateHolderFactory = fun v -> new NodeStateHolder(configuration, v, persistentDataStore)

        let raftNode =  RaftNode(translatedServerFactory, configuration, electionTimerHolder, peerSupervisorFactory, nodeStateHolderFactory)
        timerHolder.Start()

        raftNode