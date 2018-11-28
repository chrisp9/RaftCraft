﻿using RaftCraft.Domain;
using System.Threading.Tasks;

namespace RaftCraft.Interfaces
{
    public interface IPersistentDataStore
    {
        Task ApplyAsync(LogEntry[] logEntries);
        Task UpdateCurrentTermAsync(int newTerm);
        Task UpdateVotedForAsync(int candidateId);
    }
}
