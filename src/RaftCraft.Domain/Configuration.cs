namespace RaftCraft.Domain
{
    public class RaftPeer
    {
        public string Address { get; }

        public int NodeId { get; }

        public RaftPeer(string address, int nodeId)
        {
            Address = address;
            NodeId = nodeId;
        }
    }

    public class Configuration
    {
        public string SelfAddress { get; }

        public RaftPeer[] PeerAddressses { get; }

        public Configuration(
            string selfAddress, 
            RaftPeer[] peerAddresses)
        {
            SelfAddress = selfAddress ?? string.Empty;
            PeerAddressses = peerAddresses ?? new RaftPeer[0];
        }
    }
}
