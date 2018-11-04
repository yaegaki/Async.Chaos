using System;

namespace Async.Chaos
{
    public class ChaosWaitTaskAwaiter<TResult, TContinuationResult> : IChaosContinuationConsumer<TContinuationResult>, IChaosAwaiter<TResult>
    {
        private ChaosTask<TResult> task;
        public bool IsCompleted => false;

        public ChaosWaitTaskAwaiter(ChaosTask<TResult> task) => this.task = task;

        public TResult GetResult() => task.GetAwaiter().GetResult();
        public void OnCompleted(Action continuation) => continuation();

        public void SetScheduler(IChaosContinuationScheduler<TContinuationResult> scheduler)
        {
            this.task.GetAwaiter().OnCompleted(() =>
            {
                scheduler.Create(() => { });
                scheduler.Schedule(false);
            });

            scheduler.Schedule(true);
        }
    }
}
