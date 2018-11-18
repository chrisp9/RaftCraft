using System.Threading.Tasks;
using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    /// <summary>
    /// Implement this to create a server using a custom transport.
    /// </summary>
    public interface IServer
    {
        void Start();
        Task<ResponseMessage> Send(RequestMessage request);
    }
}