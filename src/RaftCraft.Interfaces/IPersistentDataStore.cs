using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IPersistentDataStore
    {
        void Apply(LogEntry[] logEntries);
        void UpdateCurrentTerm(int newTerm);
        void UpdateVotedFor(int? candidateId);
    }
}