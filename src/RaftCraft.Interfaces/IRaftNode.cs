using RaftCraft.Domain;
using System;

namespace RaftCraft.Interfaces
{
    public interface IRaftPeer
    {
        void PostResponse(RequestMessage message);
        void Start();
    }

    public interface IRaftHost
    {
        void Start(Action<RequestMessage> onMessage);
    }
}
