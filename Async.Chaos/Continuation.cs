using System;

namespace Async.Chaos
{
    public struct ChaosContinuationAwaitable<TArg, TResult>
    {
        private ChaosContinuationAwaiter<TArg, TResult> awaiter;
        public ChaosContinuationAwaitable(TArg arg) => this.awaiter = new ChaosContinuationAwaiter<TArg, TResult>(arg);
        public ChaosContinuationAwaiter<TArg, TResult> GetAwaiter() => this.awaiter;
    }

    public struct ChaosContinuationAwaitable<TArg1, TArg2, TResult>
    {
        private ChaosContinuationAwaiter<TArg1, TArg2, TResult> awaiter;
        public ChaosContinuationAwaitable((TArg1, TArg2) arg) => this.awaiter = new ChaosContinuationAwaiter<TArg1, TArg2, TResult>(arg);
        public ChaosContinuationAwaiter<TArg1, TArg2, TResult> GetAwaiter() => this.awaiter;
    }

    public struct ChaosContinuationAwaitable<TArg1, TArg2, Arg3, TResult>
    {
        private ChaosContinuationAwaiter<TArg1, TArg2, Arg3, TResult> awaiter;
        public ChaosContinuationAwaitable((TArg1, TArg2, Arg3) arg) => this.awaiter = new ChaosContinuationAwaiter<TArg1, TArg2, Arg3, TResult>(arg);
        public ChaosContinuationAwaiter<TArg1, TArg2, Arg3, TResult> GetAwaiter() => this.awaiter;
    }

    public class ChaosContinuationAwaiter<TArg, TResult> : IChaosContinuationConsumer<TResult>, IChaosAwaiter<(TArg, Func<TArg, ChaosTask<TResult>>)>
    {
        private TArg arg;
        private IChaosContinuationScheduler<TResult> scheduler;
        public bool IsCompleted => scheduler != null;

        public ChaosContinuationAwaiter(TArg arg)
        {
            this.arg = arg;
        }

        public (TArg, Func<TArg, ChaosTask<TResult>>) GetResult()
        {
            return (this.arg, _arg =>
            {
                var task = this.scheduler.Create(() => this.arg = _arg);
                this.scheduler.Schedule(true);
                return task;
            }
            );
        }

        public void OnCompleted(Action continuation)
        {
            continuation();
        }

        public void SetScheduler(IChaosContinuationScheduler<TResult> scheduler)
        {
            this.scheduler = scheduler;
            var _arg = this.arg;
            this.scheduler.Create(() => this.arg = _arg);
            this.scheduler.Schedule(true);
        }
    }

    public class ChaosContinuationAwaiter<TArg1, TArg2, TResult> : ChaosContinuationAwaiter<(TArg1, TArg2), TResult>
    {
        public ChaosContinuationAwaiter((TArg1, TArg2) arg) : base(arg) { }

        public new (TArg1, TArg2, Func<TArg1, TArg2, ChaosTask<TResult>>) GetResult()
        {
            var result = base.GetResult();
            return (result.Item1.Item1, result.Item1.Item2, (arg1, arg2) => result.Item2((arg1, arg2)));
        }
    }

    public class ChaosContinuationAwaiter<TArg1, TArg2, TArg3, TResult> : ChaosContinuationAwaiter<(TArg1, TArg2, TArg3), TResult>
    {
        public ChaosContinuationAwaiter((TArg1, TArg2, TArg3) arg) : base(arg) { }

        public new (TArg1, TArg2, TArg3, Func<TArg1, TArg2, TArg3, ChaosTask<TResult>>) GetResult()
        {
            var result = base.GetResult();
            return (result.Item1.Item1, result.Item1.Item2, result.Item1.Item3, (arg1, arg2, arg3) => result.Item2((arg1, arg2, arg3)));
        }
    }
}
