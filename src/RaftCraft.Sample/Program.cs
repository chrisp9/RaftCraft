using Newtonsoft.Json;
using RaftCraft.Domain;
using RaftCraft.Logging;
using RaftCraft.Persistence;
using RaftCraft.Transport;
using System;
using System.IO;

namespace RaftCraft.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Console.WriteLine(e.ExceptionObject);
                Console.WriteLine("CRITICAL: Process is about to abort. Press any key to exit");
                Console.ReadKey();
                Environment.Exit(-1);
            };
            var configLocation = args.Length == 0 ? "AppConfig.json" : args[0];

            var configString = File.ReadAllText(configLocation);
            var converted = JsonConvert.DeserializeObject<RaftConfiguration>(configString);

            Func<RaftPeer, TransientWebSocketClient> socketFactory = peer => TransientWebSocketClient.Create(peer.Address);

            var simpleLogger = new SimpleLogger(converted.LogFile);
            Log.SetInstance(simpleLogger);

            var node = RaftSystem.RaftSystem.Create(
                host => new RaftServer(host.Address),
                peer => new PersistentWebSocketClient(peer, socketFactory, Log.Instance),
                new SlowInMemoryDataStore(),
                converted);

            node.Start();

            Console.ReadLine();
        }
    }
}
