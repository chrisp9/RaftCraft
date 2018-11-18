using System.Threading.Tasks;
using RaftCraft.Domain;

namespace RaftCraft.Interfaces
{
    public interface IServer1
    {
        Task<ResponseMessage> Send(RequestMessage request);
    }
}