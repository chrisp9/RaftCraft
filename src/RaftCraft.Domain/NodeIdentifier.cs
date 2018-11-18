namespace RaftCraft.Domain
{
    public struct NodeIdentifier
    {
        public string Uri { get; }

        public NodeIdentifier(string uri)
        {
            Uri = uri;
        }
    }
}
