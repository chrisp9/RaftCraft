namespace RaftCraft.Domain
{
    public class PersistentLogEntry
    {
        int Term { get; }
        byte[] Data { get; }

        public PersistentLogEntry(int term, byte[] data)
        {
            Term = term;
            Data = data;
        }
    }

}
