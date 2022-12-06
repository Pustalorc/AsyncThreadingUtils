using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker;
using Pustalorc.Libraries.AsyncThreadingUtils.TaskDispatcher.QueueableTasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskDispatcher;

public abstract class SecondThreadQueue
{
    protected BackgroundWorkerWithTasks BackgroundWorker { get; }
    protected ConcurrentQueue<QueueableTask> Queue { get; }
    protected int DelayOnEmptyQueue { get; }
    protected int DelayOnItemNotReady { get; }

    protected SecondThreadQueue(BackgroundWorkerWithTasks? backgroundWorker = null)
    {
        Queue = new ConcurrentQueue<QueueableTask>();
        BackgroundWorker = backgroundWorker ?? new BackgroundWorkerCustomDefaults(ExecuteWork, ExceptionRaised);
        DelayOnEmptyQueue = 5000;
        DelayOnItemNotReady = 1000;
    }

    public virtual void Start()
    {
        BackgroundWorker.Start();
    }

    public virtual void Stop()
    {
        BackgroundWorker.Stop();
    }

    public virtual void NonBlockingStop()
    {
        BackgroundWorker.NonBlockingStop();
    }

    public virtual Action QueueAnonymousTask(Func<CancellationToken, Task> executeMethod, int delayInSeconds = 0,
        bool repeating = false)
    {
        var t = new QueuedAnonymousTask(executeMethod, delayInSeconds, repeating);
        Queue.Enqueue(t);
        return t.Cancel;
    }

    public virtual void QueueTask(QueueableTask task)
    {
        Queue.Enqueue(task);
    }

    protected abstract void ExceptionRaised(Exception exception);

    protected virtual async Task ExecuteWork(CancellationToken token)
    {
        if (!Queue.TryDequeue(out var task))
        {
            await Task.Delay(DelayOnEmptyQueue, token);
            return;
        }

        if (task.IsCancelled || token.IsCancellationRequested)
            return;

        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (task.UnixTimeStampToExecute > currentUnixTimestamp)
        {
            if (!Queue.TryPeek(out _))
                await Task.Delay(DelayOnItemNotReady, token);

            Queue.Enqueue(task);
            return;
        }

        await task.ExecuteWithCancelCheck(token);

        if (task.IsRepeating && !task.IsCancelled)
        {
            task.ResetTimestamp();
            Queue.Enqueue(task);
        }
    }
}