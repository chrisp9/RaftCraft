using RaftCraft.Domain;
using System;

namespace RaftCraft.Interfaces
{
    public interface IRaftHost
    {
        void Start(Action<RaftMessage> onMessage);
        void Stop();
    }
}
