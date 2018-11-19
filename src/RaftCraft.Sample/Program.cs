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
            var configLocation = args.Length == 0 ? "AppConfig.json" : args[0];

            var configString = File.ReadAllText(configLocation);
            var converted = JsonConvert.DeserializeObject<RaftConfiguration>(configString);

            Func<RaftPeer, TransientWebSocketClient> socketFactory = peer => TransientWebSocketClient.Create(peer.Address);

            var raftNode = new RaftNode(
                host => new RaftServer(host.Address), 
                peer => new PersistentWebSocketClient(peer, socketFactory), 
                converted);

            raftNode.Start();

            Console.ReadLine();
        }
    }
}
