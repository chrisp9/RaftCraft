using System;

namespace RaftCraft.Domain
{
    [Serializable]
    public class RaftPeer
    {
        public int NodeId { get; set; }

        public string Address { get; set; }

        public RaftPeer(int nodeId, string address)
        {
            NodeId = nodeId;
            Address = address;
        }
    }

    [Serializable]
    public class RaftHost
    {
        public int NodeId { get; set; }

        public string Address { get; set; }

        public RaftHost(int nodeId, string address)
        {
            NodeId = nodeId;
            Address = address;
        }
    }

    [Serializable]
    public class RaftConfiguration
    {
        public RaftHost Self { get; set; }

        public RaftPeer[] Peers { get; set; }

        public string LogFile { get; set; }

        public int GlobalTimerTickInterval { get; set; }

        public int ElectionTimeout { get; set; }

        public int RequestPipelineRetryInterval { get; set; }

        public RaftConfiguration(
            RaftHost self, 
            RaftPeer[] peers,
            string logFile,
            int globalTimerTickInterval,
            int electionTimeout,
            int requestPipelineRetryInterval)
        {
            Self = self;
            Peers = peers ?? new RaftPeer[0];
            LogFile = logFile;

            GlobalTimerTickInterval = globalTimerTickInterval;
            ElectionTimeout = electionTimeout;
            RequestPipelineRetryInterval = requestPipelineRetryInterval;
        }
    }
}
