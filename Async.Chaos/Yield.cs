using System;

namespace Async.Chaos
{
    public class ChaosYieldAwaiter<T> : IChaosContinuationConsumer<T>, IChaosAwaiter<ChaosUnit>
    {
        public bool IsCompleted => false;

        public ChaosUnit GetResult() => default(ChaosUnit);
        public void OnCompleted(Action continuation) => continuation();

        public void SetScheduler(IChaosContinuationScheduler<T> scheduler)
        {
            scheduler.Create(() => { });
            scheduler.Schedule(true);
        }
    }
}
