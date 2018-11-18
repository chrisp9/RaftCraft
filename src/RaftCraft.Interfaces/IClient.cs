using RaftCraft.Domain;
using System.Threading.Tasks;

namespace RaftCraft.Interfaces
{
    public interface IClient
    {
        Task<ResponseMessage> Handle(RequestMessage request);
    }
}
