using System;

namespace Async.Chaos
{
    public class ChaosConcurrentAwaiter<T> : IChaosContinuationConsumer<T>, IChaosAwaiter<bool>
    {
        private bool isParent;
        public bool IsCompleted => false;

        public bool GetResult() => this.isParent;
        public void OnCompleted(Action continuation) => continuation();

        public void SetScheduler(IChaosContinuationScheduler<T> scheduler)
        {
            scheduler.Create(() => this.isParent = true);
            scheduler.Create(() => this.isParent = false);
            scheduler.Schedule(true);
        }
    }
}
