using RaftCraft.Interfaces;
using System;
using System.IO;

namespace RaftCraft.Sample
{
    // TODO not efficient. Replace with a proper logger.
    public class SimpleLogger : ILogger
    {
        private string _logFile;
        private readonly object _lock = new object();

        public SimpleLogger(string logFile)
        {
            _logFile = logFile;
        }

        private string Prefix(string detail)
        {
            return $"{DateTime.UtcNow}: [{detail}] - ";
        }

        public void Error(string text, Exception e)
        {
            lock(_lock)
            {
                try
                {
                    File.AppendAllText(_logFile, Prefix("Error") + e + text + "\r\n");
                }
                catch(Exception) { }
            }
        }

        public void Info(string text)
        {
            lock(_lock)
            {
                try
                {
                    File.AppendAllText(_logFile, Prefix("Info") + text + "\r\n");
                }
                catch(Exception) { }
            }
        }

        public void Warn(string text)
        {
            lock(_lock)
            {
                try
                {
                    File.AppendAllText(_logFile, Prefix("Warn") + text + "\r\n");
                }
                catch(Exception) { }
            }
        }

        public void Debug(string text)
        {
            lock(_lock)
            {
                lock (_lock)
                {
                    try
                    {
                        File.AppendAllText(_logFile, Prefix("Debug") + text + "\r\n");
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
