namespace RaftCraft.Domain
{
    public class PersistentLogEntry
    {
        public int Index { get; }
        public int Term { get; }
        public byte[] Data { get; }

        public PersistentLogEntry(int index, int term, byte[] data)
        {
            Term = term;
            Data = data;
            Index = index;
        }
    }
}
