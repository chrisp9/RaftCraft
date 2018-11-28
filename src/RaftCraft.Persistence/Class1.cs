using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaftCraft.Persistence
{
    public class SlowInMemoryDataStore : IPersistentDataStore
    {
        private List<PersistentLogEntry> _allEntries = new List<PersistentLogEntry>();

        public Task ApplyAsync(PersistentLogEntry[] logEntries)
        {
            _allEntries = logEntries;
        }
    }
}
