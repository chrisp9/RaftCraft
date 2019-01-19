namespace RaftCraft.Domain
{
    public struct TimerTick
    {
        public long Granularity { get; set; }

        public long CurrentTick { get; set; }

        public TimerTick(long granularity, long currentTick)
        {
            Granularity = granularity;
            CurrentTick = currentTick;
        }
    }
}
