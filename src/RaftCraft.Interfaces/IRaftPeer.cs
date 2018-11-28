using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IRaftPeer
    {
        void Post(RaftMessage message);
        void Start();
    }
}
