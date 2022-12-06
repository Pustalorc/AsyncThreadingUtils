using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Implementations;
using Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.QueueableTasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue;

/// <summary>
///     A management class for queueing <see cref="Task" />s on a <see cref="BackgroundWorkerWithTasks" />.
/// </summary>
[UsedImplicitly]
public abstract class TaskQueueManager
{
    /// <summary>
    ///     The <see cref="BackgroundWorkerWithTasks" /> that will execute the tasks in the <see cref="Queue" />.
    /// </summary>
    [UsedImplicitly]
    protected BackgroundWorkerWithTasks BackgroundWorker { get; }

    /// <summary>
    ///     The <see cref="ConcurrentQueue{T}" /> for this manager.
    /// </summary>
    /// <remarks>
    ///     This is concurrent as we will be queueing and dequeueing on different threads, avoiding the use of
    ///     <see langword="lock" />s.
    /// </remarks>
    [UsedImplicitly]
    protected ConcurrentQueue<QueueableTask> Queue { get; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the queue currently be empty.
    /// </summary>
    [UsedImplicitly]
    protected int DelayOnEmptyQueue { get; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the current item not be ready for execution and
    ///     be the only item in the queue.
    /// </summary>
    [UsedImplicitly]
    protected int DelayOnItemNotReadyAndSolo { get; }

    /// <summary>
    ///     The constructor for the manager.
    /// </summary>
    /// <param name="delayOnEmptyQueue">
    ///     The delay (in milliseconds) before execution should resume, should the queue currently
    ///     be empty.
    /// </param>
    /// <param name="delayOnItemNotReadyAndSolo">
    ///     The delay (in milliseconds) before execution should resume, should the current
    ///     item not be ready for execution and be the only item in the queue.
    /// </param>
    /// <param name="repeating">If the background worker should repeat execution indefinitely.</param>
    /// <param name="startThrowsExceptions">
    ///     If the background worker should throw an exception when <see cref="Start" /> is
    ///     called and the worker is busy
    /// </param>
    /// <param name="stopThrowsExceptions">
    ///     If the background worker should throw an exception when <see cref="Stop" /> or
    ///     <see cref="NonBlockingStop" /> is called and the worker is not currently executing
    /// </param>
    protected TaskQueueManager(int delayOnEmptyQueue = 5000, int delayOnItemNotReadyAndSolo = 1000,
        bool repeating = true, bool startThrowsExceptions = true, bool stopThrowsExceptions = true)
    {
        Queue = new ConcurrentQueue<QueueableTask>();
        BackgroundWorker = new BackgroundWorkerCustomDefaults(ExecuteWork, ExceptionRaised, repeating,
            startThrowsExceptions, stopThrowsExceptions);
        DelayOnEmptyQueue = delayOnEmptyQueue;
        DelayOnItemNotReadyAndSolo = delayOnItemNotReadyAndSolo;
    }

    /// <summary>
    ///     Starts the manager for execution and processing of the queue.
    /// </summary>
    /// <remarks>
    ///     This does not reset or clear the queue.
    /// </remarks>
    [UsedImplicitly]
    public virtual void Start()
    {
        BackgroundWorker.Start();
    }

    /// <summary>
    ///     Stops the manager from executing further and processing the queue.
    /// </summary>
    /// <remarks>
    ///     This does not reset or clear the queue.
    /// </remarks>
    [UsedImplicitly]
    public virtual void Stop()
    {
        BackgroundWorker.Stop();
    }

    /// <summary>
    ///     Stops the manager from executing further and processing the queue in a non blocking manner.
    /// </summary>
    /// <remarks>
    ///     This does not reset or clear the queue.
    /// </remarks>
    [UsedImplicitly]
    public virtual void NonBlockingStop()
    {
        BackgroundWorker.NonBlockingStop();
    }

    /// <summary>
    ///     Queues an anonymous task for execution.
    /// </summary>
    /// <param name="functionToExecute">The <see cref="T:System.Func`2" /> that will be executed.</param>
    /// <param name="delay">The delay (in seconds) before this task should be executed.</param>
    /// <param name="repeating">A boolean determining if this task should repeat after execution.</param>
    /// <returns>An <see cref="Action" /> representing the method to cancel the anonymous task from executing.</returns>
    [UsedImplicitly]
    public virtual Action QueueAnonymousTask(Func<CancellationToken, Task> functionToExecute, int delay = 0,
        bool repeating = false)
    {
        var t = new QueuedAnonymousTask(functionToExecute, delay, repeating);
        QueueTask(t);
        return t.Cancel;
    }

    /// <summary>
    ///     Queues a <see cref="QueueableTask" /> for execution.
    /// </summary>
    /// <param name="task">The <see cref="QueueableTask" /> to queue.</param>
    [UsedImplicitly]
    public virtual void QueueTask(QueueableTask task)
    {
        Queue.Enqueue(task);
    }

    /// <summary>
    ///     The method called if an <see cref="Exception" /> is raised.
    /// </summary>
    /// <param name="exception">The <see cref="Exception" /> that was raised.</param>
    protected abstract void ExceptionRaised(Exception exception);

    /// <summary>
    ///     The method that performs the work for the manager.
    /// </summary>
    /// <param name="token">The cancellation token to dynamically cancel execution when requested.</param>
    /// <remarks>
    ///     This method should check <see cref="CancellationToken.IsCancellationRequested" /> whenever possible.
    ///     If the queue is empty, the method should delay by <see cref="DelayOnEmptyQueue" /> milliseconds.
    ///     If the task that has been dequeued is cancelled, the task should be dropped.
    ///     A check using <see cref="DateTimeOffset.ToUnixTimeSeconds" /> for the task's unix timestamp should be used.
    ///     If the task is not ready for execution, and there are no more tasks in the queue, the method should delay by
    ///     <see cref="DelayOnItemNotReadyAndSolo" /> milliseconds.
    ///     If the task is repeating, the method should request the task to reset its timestamp and re-queue the task.
    /// </remarks>
    protected virtual async Task ExecuteWork(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        if (!Queue.TryDequeue(out var task))
        {
            await Task.Delay(DelayOnEmptyQueue, token);
            return;
        }

        if (task.IsCancelled)
            return;

        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (task.UnixTimeStampToExecute > currentUnixTimestamp)
        {
            if (!Queue.TryPeek(out _))
                await Task.Delay(DelayOnItemNotReadyAndSolo, token);

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