using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaftCraft.Persistence
{
    public class SlowInMemoryDataStore : IPersistentDataStore
    {
        private List<PersistentLogEntry> _allEntries = new List<PersistentLogEntry>();

        private int _currentTerm = 0;
        private int _votedFor = 0;

        public Task ApplyAsync(PersistentLogEntry[] logEntries)
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
