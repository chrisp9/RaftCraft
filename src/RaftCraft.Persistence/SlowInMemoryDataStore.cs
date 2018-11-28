using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaftCraft.Persistence
{
    public class SlowInMemoryDataStore : IPersistentDataStore
    {
        private List<LogEntry> _allEntries = new List<LogEntry>();

        private int _currentTerm = 0;
        private int _votedFor = 0;

        public Task ApplyAsync(LogEntry[] logEntries)
        {
            _allEntries.AddRange(logEntries);
            return Task.FromResult(0);
        }

        public Task UpdateCurrentTermAsync(int newTerm)
        {
            _currentTerm = newTerm;
            return Task.FromResult(0);
        }

        public Task UpdateVotedForAsync(int candidateId)
        {
            _votedFor = candidateId;
            return Task.FromResult(0);
        }
    }
}
