using Newtonsoft.Json;
using RaftCraft.Domain;
using RaftCraft.Raft;
using RaftCraft.Transport;
using System;
using System.IO;

namespace RaftCraft.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var configString = File.ReadAllText("AppConfig.json");
            var converted = JsonConvert.DeserializeObject<RaftConfiguration>(configString);

            Func<RaftPeer, TransientWebSocketClient> socketFactory = peer => TransientWebSocketClient.Create(peer.Address);

            var raftNode = new RaftNode(
                host => new RaftServer(host.Address), 
                client => new PersistentWebSocketClient(client, socketFactory), 
                converted);

            raftNode.Start();

            Console.ReadLine();
        }
    }
}
