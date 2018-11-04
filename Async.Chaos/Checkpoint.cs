using System;

namespace Async.Chaos
{
    public class ChaosCheckPointAwaiter<T> : IChaosContinuationConsumer<T>, IChaosAwaiter<Func<ChaosAwaitable<ChaosBreakAwaiter<T>>>>
    {
        private IChaosContinuationScheduler<T> scheduler;
        public bool IsCompleted => scheduler != null;

        public Func<ChaosAwaitable<ChaosBreakAwaiter<T>>> GetResult()
        {
            return () =>
            {
                var task = this.scheduler.Create(() => { });
                this.scheduler.Schedule(true);
                return ChaosTask.Break<T>();
            };
        }

        public void OnCompleted(Action continuation)
        {
            continuation();
        }

        public void SetScheduler(IChaosContinuationScheduler<T> scheduler)
        {
            this.scheduler = scheduler;
            this.scheduler.Create(() => { });
            this.scheduler.Schedule(true);
        }
    }
}
