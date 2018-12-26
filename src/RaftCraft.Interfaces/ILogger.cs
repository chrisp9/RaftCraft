using System;

namespace RaftCraft.Interfaces
{
    public interface ILogger
    {
        void Error(string text, Exception e);
        void Warn(string text);
        void Info(string text);
    }
}
