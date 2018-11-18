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

        public RaftConfiguration(
            RaftHost self, 
            RaftPeer[] peers)
        {
            Self = self;
            Peers = peers ?? new RaftPeer[0];
        }
    }
}
