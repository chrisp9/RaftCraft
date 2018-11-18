using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IRaftPeer
    {
        void Post(RequestMessage message);
        void Start();
    }
}
