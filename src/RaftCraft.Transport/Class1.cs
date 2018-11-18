using RaftCraft.Domain;
using RaftCraft.Interfaces;
using System;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RaftCraft.Transport
{
    public class RaftServer : WebSocketBehavior, IServer
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            //e.Data;
        }

        public Task<ResponseMessage> Send(RequestMessage request)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {

        }
    }
}
