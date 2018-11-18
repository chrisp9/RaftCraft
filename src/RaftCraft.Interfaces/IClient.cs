using RaftCraft.Domain;
using System.Threading.Tasks;

namespace RaftCraft.Interfaces
{
    /// <summary>
    /// Implement this to create a client using a custom transport. Note that implementations must handle automatic reconnects.
    /// </summary>
    public interface IClient
    {
        Task<ResponseMessage> Handle(RequestMessage request);

        void Start();
    }
}
