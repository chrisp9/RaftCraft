using Newtonsoft.Json;
using RaftCraft.Domain;
using System.IO;

namespace RaftCraft.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var configString = File.ReadAllText("AppConfig.json");
            var converted = JsonConvert.DeserializeObject<RaftConfiguration>(configString);
            var item = JsonConvert.SerializeObject(configuration);
        }
    }
}
