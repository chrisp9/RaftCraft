using RaftCraft.Interfaces;
using System;
using System.Linq;
using System.IO;

namespace RaftCraft.Sample
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    // TODO not efficient. Replace with a proper logger.
    public class SimpleLogger : ILogger
    {
        private string _logFile;
        private readonly object _lock = new object();

        private static readonly int PadAmount;

        static SimpleLogger()
        {
            PadAmount = Enum.GetValues(typeof(LogLevel))
                .Cast<object>()
                .Select(v => v.ToString().Length)
                .Max();
        }

        public SimpleLogger(string logFile)
        {
            _logFile = logFile;
        }

        private string Pad(string detail)
        {
            return detail.PadLeft(PadAmount);
        }

        private string Prefix(LogLevel logLevel)
        {
            return $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}: [{Pad(logLevel.ToString())}] - ";
        }

        public void Error(string text, Exception e)
        {
            lock(_lock)
            {
                try
                {
                    File.AppendAllText(_logFile, Prefix(LogLevel.Error) + e + text + "\r\n");
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
                    File.AppendAllText(_logFile, Prefix(LogLevel.Info) + text + "\r\n");
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
                    File.AppendAllText(_logFile, Prefix(LogLevel.Warn) + text + "\r\n");
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
                        File.AppendAllText(_logFile, Prefix(LogLevel.Debug) + text + "\r\n");
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
