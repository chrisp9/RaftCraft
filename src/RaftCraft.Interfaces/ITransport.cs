using RaftCraft.Domain;
using System.Threading.Tasks;

namespace RaftCraft.Interfaces
{
    public interface IServer
    {
        Task<ResponseMessage> Send(RequestMessage request);
    }

    public interface IClient
    {
        void Send(ResponseMessage response);
    }
}