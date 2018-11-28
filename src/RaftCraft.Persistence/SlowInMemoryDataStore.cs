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

        public int LastLogIndex { get; private set; }
        public int LastLogTerm { get; private set; }

        public void Apply(LogEntry[] logEntries)
        {
            _allEntries.AddRange(logEntries);

            foreach(var entry in logEntries)
            {
                if (entry.Index > LastLogIndex)
                    LastLogIndex = entry.Index;

                if (entry.Term > LastLogTerm)
                    LastLogTerm = entry.Term;
            }
        }

        public void Update(int newTerm, int? votedFor)
        {
            // TODO Real version must be transactional.
            _currentTerm = newTerm;
            _votedFor = votedFor;
        }
    }
}
