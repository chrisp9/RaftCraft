using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IPersistentDataStore
    {
        void Apply(LogEntry[] logEntries);
        void Update(int newTerm, int? votedFor);

        int LastLogIndex { get; }
        int LastLogTerm { get; }
    }
}