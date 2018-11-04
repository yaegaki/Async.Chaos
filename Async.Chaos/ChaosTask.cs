using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Async.Chaos
{
    public struct ChaosUnit
    {
    }

    public struct ChaosTask
    {
        public static ChaosContinuationAwaitable<T, T> Continuation<T>(T arg) => new ChaosContinuationAwaitable<T, T>(arg);
        public static ChaosContinuationAwaitable<TArg, TResult> Continuation<TArg, TResult>(TArg arg) => new ChaosContinuationAwaitable<TArg, TResult>(arg);
        public static ChaosContinuationAwaitable<TArg1, TArg2, TResult> Continuation<TArg1, TArg2, TResult>(TArg1 arg1, TArg2 arg2) => new ChaosContinuationAwaitable<TArg1, TArg2, TResult>((arg1, arg2));
        public static ChaosContinuationAwaitable<TArg1, TArg2, TArg3, TResult> Continuation<TArg1, TArg2, TArg3, TResult>(TArg1 arg1, TArg2 arg2, TArg3 arg3) => new ChaosContinuationAwaitable<TArg1, TArg2, TArg3, TResult>((arg1, arg2, arg3));

        public static ChaosAwaitable<ChaosYieldAwaiter<T>> Yield<T>() => ChaosAwaitable.Create(new ChaosYieldAwaiter<T>());

        public static ChaosAwaitable<ChaosWaitNextAwaiter<T>> WaitNext<T>() => ChaosAwaitable.Create(new ChaosWaitNextAwaiter<T>());

        public static ChaosAwaitable<ChaosWaitTaskAwaiter<ChaosUnit, T>> WaitTask<T>(ChaosTask<ChaosUnit> task) => ChaosAwaitable.Create(new ChaosWaitTaskAwaiter<ChaosUnit, T>(task));
        public static ChaosAwaitable<ChaosWaitTaskAwaiter<TResult, TContinuationResult>> WaitTask<TResult, TContinuationResult>(ChaosTask<TResult> task) => ChaosAwaitable.Create(new ChaosWaitTaskAwaiter<TResult, TContinuationResult>(task));

        public static ChaosAwaitable<ChaosConcurrentAwaiter<T>> Concurrent<T>() => ChaosAwaitable.Create(new ChaosConcurrentAwaiter<T>());

        public static ChaosAwaitable<ChaosCheckPointAwaiter<T>> Checkpoint<T>() => ChaosAwaitable.Create(new ChaosCheckPointAwaiter<T>());

        public static ChaosAwaitable<ChaosBreakAwaiter<T>> Break<T>() => ChaosAwaitable.Create(new ChaosBreakAwaiter<T>());
    }

    [AsyncMethodBuilder(typeof(AsyncChaosMethodBuilder<>))]
    public struct ChaosTask<T>
    {
        private IChaosAwaiter<T> awaiter;

        public bool IsCompleted => GetAwaiter().IsCompleted;

        public ChaosTask(IChaosAwaiter<T> awaiter)
        {
            this.awaiter = awaiter;
        }

        public Awaiter GetAwaiter() => new Awaiter(this.awaiter);

        public struct Awaiter : IChaosAwaiter<T>
        {
            private IChaosAwaiter<T> awaiter;
            public Awaiter(IChaosAwaiter<T> awaiter) => this.awaiter = awaiter;

            public bool IsCompleted => awaiter != null ? awaiter.IsCompleted : true;
            public T GetResult() => awaiter != null ? awaiter.GetResult() : default(T);

            public void OnCompleted(Action continuation)
            {
                if (this.awaiter != null)
                {
                    this.awaiter.OnCompleted(continuation);
                }
                else
                {
                    continuation();
                }
            }
        }

        public async ChaosTask<T> ContinueWith(Func<T, ChaosTask<T>> func)
        {
            var result = await this;
            return await func(result);
        }
    }

    public interface IChaosAwaiter<T> : INotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }

    public class ChaosAwaiter<T> : IChaosAwaiter<T>
    {
        private T result;
        private List<Action> continuations;

        public bool IsCompleted { get; private set; }

        public void SetResult(T result)
        {
            this.result = result;
            this.IsCompleted = true;

            if (this.continuations != null)
            {
                foreach (var continuation in continuations)
                {
                    try
                    {
                        continuation();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public T GetResult() => result;

        public void OnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
            }
            else
            {
                if (this.continuations == null)
                {
                    this.continuations = new List<Action>(1);
                    this.continuations.Add(continuation);
                }
                else
                {
                    this.continuations.Add(continuation);
                }
            }
        }
    }

    public struct ChaosAwaitable
    {
        public static ChaosAwaitable<TAwaiter> Create<TAwaiter>(TAwaiter awaiter) => new ChaosAwaitable<TAwaiter>(awaiter);
    }

    public struct ChaosAwaitable<TAwaiter>
    {
        private TAwaiter awaiter;
        public ChaosAwaitable(TAwaiter awaiter) => this.awaiter = awaiter;
        public TAwaiter GetAwaiter() => this.awaiter;
    }


    public interface IChaosContinuationConsumer<T>
    {
        void SetScheduler(IChaosContinuationScheduler<T> scheduler);
    }


    public interface IChaosContinuationScheduler<T>
    {
        ChaosTask<T> Create(Action prepare);
        void Schedule(bool hasPriority);
        void Abandon();
    }
}
