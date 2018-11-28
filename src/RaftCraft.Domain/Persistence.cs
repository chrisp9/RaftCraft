using ProtoBuf;
using System;

namespace RaftCraft.Domain
{
    [ProtoContract]
    [Serializable]
    public class LogEntry
    {
        [ProtoMember(1)]
        public int Index { get; }

        [ProtoMember(2)]
        public int Term { get; }

        [ProtoMember(3)]
        public byte[] Data { get; }

        public LogEntry(int index, int term, byte[] data)
        {
            Term = term;
            Data = data;
            Index = index;
        }

        public LogEntry() { }
    }
}
