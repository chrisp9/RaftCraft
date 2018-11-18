using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IRaftPeer
    {
        void PostResponse(RequestMessage message);
        void Start();
    }
}
