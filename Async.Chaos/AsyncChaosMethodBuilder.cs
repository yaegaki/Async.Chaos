using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Async.Chaos
{
    public class AsyncChaosMethodBuilder<T>
    {
        public static AsyncChaosMethodBuilder<T> Create() => new AsyncChaosMethodBuilder<T>();

        public ChaosTask<T> Task => new ChaosTask<T>(resultAwaiter);
        private ChaosAwaiter<T> resultAwaiter = new ChaosAwaiter<T>();
        private Queue<ChaosContinuationContext> pendingQueue = new Queue<ChaosContinuationContext>();
        private ChaosContinuationContext currentContext;
        private bool isScheduling;
        private bool scheduleRequired;
        private bool isRunning;
        private object gate = new object();

        private static readonly MethodInfo MemberwiseCloneInfo = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        private static TStateMachine CloneStateMachine<TStateMachine>(TStateMachine stateMachine) => (TStateMachine)MemberwiseCloneInfo.Invoke(stateMachine, Array.Empty<object>());

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => stateMachine.MoveNext();

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void SetException(Exception exception) { }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            switch (awaiter)
            {
                case IChaosContinuationConsumer<T> consumer:
                    {
                        // save and capture current StateMachine.
                        var clonedStateMachine = CloneStateMachine(stateMachine);

                        var scheduler = new Scheduler(prepare =>
                        {
                            var context = new ChaosContinuationContext()
                            {
                                awaiter = new ChaosAwaiter<T>(),
                                continuation = () =>
                                {
                                    var _clonedStateMachine = clonedStateMachine;
                                    clonedStateMachine = CloneStateMachine(_clonedStateMachine);
                                    prepare();
                                    _clonedStateMachine.MoveNext();
                                },
                            };

                            lock (this.gate)
                            {
                                this.pendingQueue.Enqueue(context);
                            }
                            return new ChaosTask<T>(context.awaiter);
                        }, this.Schedule, this.Abandon);

                        consumer.SetScheduler(scheduler);
                    }
                    break;
                default:
                    awaiter.OnCompleted(stateMachine.MoveNext);
                    break;
            }
        }

        public void SetResult(T result)
        {
            if (this.currentContext != null)
            {
                var _currentContext = this.currentContext;
                this.currentContext = this.currentContext.prev;
                _currentContext.awaiter.SetResult(result);
            }
            else
            {
                this.resultAwaiter.SetResult(result);
            }
        }

        private void Schedule(bool hasPriority)
        {
            lock (this.gate)
            {
                if (this.isRunning)
                {
                    if (!hasPriority)
                    {
                        return;
                    }

                    if (this.isScheduling)
                    {
                        if (hasPriority)
                        {
                            this.scheduleRequired = true;
                        }
                        return;
                    }

                }

                this.isScheduling = true;
            }

            var guard = true;
            this.scheduleRequired = true;
            this.isRunning = true;
            while (true)
            {
                ChaosContinuationContext context;
                lock (this.gate)
                {
                    var _scheduleRequired = this.scheduleRequired;
                    this.scheduleRequired = false;

                    if (!_scheduleRequired || this.pendingQueue.Count == 0)
                    {
                        if (this.pendingQueue.Count == 0)
                        {
                            Abandon();
                        }
                        this.isScheduling = false;
                        break;
                    }
                    context = this.pendingQueue.Dequeue();
                }
                context.awaiter.OnCompleted(() =>
                {
                    if (guard)
                    {
                        if (this.pendingQueue.Count == 0)
                        {
                            guard = false;
                            this.resultAwaiter.SetResult(context.awaiter.GetResult());
                        }

                        Schedule(true);
                    }
                });

                context.prev = this.currentContext;
                this.currentContext = context;
                context.continuation();
            }
        }

        private void Abandon()
        {
            lock (this.gate)
            {
                this.isRunning = false;
            }
        }

        private class ChaosContinuationContext
        {
            public ChaosAwaiter<T> awaiter;
            public Action continuation;
            public ChaosContinuationContext prev;
        }

        private class Scheduler : IChaosContinuationScheduler<T>
        {
            Func<Action, ChaosTask<T>> create;
            Action<bool> schedule;
            Action abandon;

            public Scheduler(Func<Action, ChaosTask<T>> create, Action<bool> schedule, Action abandon)
            {
                this.create = create;
                this.schedule = schedule;
                this.abandon = abandon;
            }

            public ChaosTask<T> Create(Action prepare)
            {
                return this.create(prepare);
            }

            public void Schedule(bool hasPriority)
            {
                this.schedule(hasPriority);
            }

            public void Abandon()
            {
                this.abandon();
            }
        }
    }
}
