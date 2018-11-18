namespace RaftCraft.Domain
{
    public class Configuration
    {
        public string SelfAddress { get; set; }

        public string[] PeerAddressses { get; set; }

        public Configuration(
            string selfAddress, 
            string[] peerAddresses)
        {
            SelfAddress = selfAddress ?? string.Empty;
            PeerAddressses = peerAddresses ?? new string[0];
        }
    }
}
