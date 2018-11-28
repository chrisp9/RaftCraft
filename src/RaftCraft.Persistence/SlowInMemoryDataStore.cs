using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System.Collections.Generic;

namespace RaftCraft.Persistence
{
    public class SlowInMemoryDataStore : IPersistentDataStore
    {
        private List<LogEntry> _allEntries = new List<LogEntry>();

        private int _currentTerm = 0;
        private int? _votedFor = 0;

        public void Apply(LogEntry[] logEntries)
        {
            _allEntries.AddRange(logEntries);
        }

        public void UpdateCurrentTerm(int newTerm)
        {
            _currentTerm = newTerm;
        }

        public void UpdateVotedFor(int? candidateId)
        {
            _votedFor = candidateId;
        }
    }
}
