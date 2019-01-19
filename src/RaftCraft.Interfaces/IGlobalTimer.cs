using RaftCraft.Domain;
using System;

namespace RaftCraft.Interfaces
{
    public interface IGlobalTimer
    {
        IObservable<TimerTick> Observable();
        void Start();
        void Stop();
    }
}
