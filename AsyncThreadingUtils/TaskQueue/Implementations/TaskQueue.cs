using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Implementations;
using Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.QueueableTasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.Implementations;

/// <summary>
///     A class for queueing <see cref="Task" />s on a <see cref="BackgroundWorkerWithTasks" />.
/// </summary>
[UsedImplicitly]
public class TaskQueue
{
    /// <summary>
    ///     The <see cref="BackgroundWorkerWithTasks" /> that will execute the <see cref="QueueableTask" />s in the
    ///     <see cref="Queue" />.
    /// </summary>
    [UsedImplicitly]
    protected BackgroundWorkerWithTasks BackgroundWorker { get; }

    /// <summary>
    ///     The <see cref="ConcurrentQueue{T}" /> for the class.
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
    public int DelayOnEmptyQueue { get; set; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the current item not be ready for execution and
    ///     be the only item in the queue.
    /// </summary>
    [UsedImplicitly]
    public int DelayOnItemNotReadyAndSolo { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="Action{T}" /> that will be called when an <see cref="Exception" /> is raised.
    /// </summary>
    /// <remarks>
    ///     This property gets and sets the value directly from the underlying BackgroundWorker.
    /// </remarks>
    [UsedImplicitly]
    public Action<Exception> ExceptionRaised
    {
        get => BackgroundWorker.ExceptionRaised;
        set => BackgroundWorker.ExceptionRaised = value;
    }

    /// <summary>
    ///     If this is set to <see langword="true" />, calling <see cref="Start" /> when the worker is busy will cause an
    ///     exception to be raised.
    /// </summary>
    /// <remarks>
    ///     This property gets and sets the value directly from the underlying BackgroundWorker.
    /// </remarks>
    [UsedImplicitly]
    public bool StartThrowsExceptions
    {
        get => BackgroundWorker.StartThrowsExceptions;
        set => BackgroundWorker.StartThrowsExceptions = value;
    }

    /// <summary>
    ///     If this is set to <see langword="true" />, calling <see cref="Stop" /> or <see cref="NonBlockingStop" /> when the
    ///     worker is not executing will cause an exception to be raised.
    /// </summary>
    /// <remarks>
    ///     This property gets and sets the value directly from the underlying BackgroundWorker.
    /// </remarks>
    [UsedImplicitly]
    public bool StopThrowsExceptions
    {
        get => BackgroundWorker.StopThrowsExceptions;
        set => BackgroundWorker.StopThrowsExceptions = value;
    }

    /// <summary>
    ///     The default constructor for this class. It will default all values to non-null, and exceptions will be thrown by
    ///     default.
    /// </summary>
    public TaskQueue()
    {
        Queue = new ConcurrentQueue<QueueableTask>();

        BackgroundWorker = new BackgroundWorkerWithTasks
        {
            FunctionToExecute = ExecuteWork,
            Repeating = true
        };

        DelayOnEmptyQueue = 5000;
        DelayOnItemNotReadyAndSolo = 1000;
    }

    /// <summary>
    ///     The constructor for the class.
    /// </summary>
    /// <param name="exceptionRaised">
    ///     The <see cref="Action{T}" /> that will be called if an exception occurs during execution of tasks.
    /// </param>
    /// <param name="delayOnEmptyQueue">
    ///     The delay (in milliseconds) before execution should resume, should the queue currently
    ///     be empty.
    /// </param>
    /// <param name="delayOnItemNotReadyAndSolo">
    ///     The delay (in milliseconds) before execution should resume, should the current
    ///     item not be ready for execution and be the only item in the queue.
    /// </param>
    /// <param name="startThrowsExceptions">
    ///     If the background worker should throw an exception when <see cref="Start" /> is
    ///     called and the worker is busy
    /// </param>
    /// <param name="stopThrowsExceptions">
    ///     If the background worker should throw an exception when <see cref="Stop" /> or
    ///     <see cref="NonBlockingStop" /> is called and the worker is not currently executing
    /// </param>
    public TaskQueue(Action<Exception> exceptionRaised, int delayOnEmptyQueue = 5000,
        int delayOnItemNotReadyAndSolo = 1000,
        bool startThrowsExceptions = true, bool stopThrowsExceptions = true)
    {
        Queue = new ConcurrentQueue<QueueableTask>();

        BackgroundWorker = new BackgroundWorkerWithTasks(ExecuteWork, exceptionRaised, true, startThrowsExceptions,
            stopThrowsExceptions);

        DelayOnEmptyQueue = delayOnEmptyQueue;
        DelayOnItemNotReadyAndSolo = delayOnItemNotReadyAndSolo;
    }

    /// <summary>
    ///     Starts the processing of the queue for execution.
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
    ///     Stops the queue from processing further.
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
    ///     Stops the queue from processing further in a non blocking manner.
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
    /// <param name="delay">The delay (in milliseconds) before this task should be executed.</param>
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
    ///     Executes work from the queue.
    /// </summary>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    [UsedImplicitly]
    protected virtual async Task ExecuteWork(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        var task = await GetNextTask(token);

        if (task == null || !await IsTaskValid(task, token))
            return;

        if (token.IsCancellationRequested)
        {
            Queue.Enqueue(task);
            return;
        }

        await ExecuteTask(task, token);
        await CheckAndQueueRepeatingTask(task, token);
    }

    /// <summary>
    ///     Retrieves the next task to be executed from the queue.
    /// </summary>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    /// <returns>A <see cref="QueueableTask" /> if there was one in the queue, <see langword="null" /> otherwise.</returns>
    [UsedImplicitly]
    protected virtual async Task<QueueableTask?> GetNextTask(CancellationToken token)
    {
        if (Queue.TryDequeue(out var task))
            return task;

        await Task.Delay(DelayOnEmptyQueue, token);
        return null;
    }

    /// <summary>
    ///     Checks if the <see cref="QueueableTask" /> is valid and ready for execution.
    /// </summary>
    /// <param name="task">The <see cref="QueueableTask" /> to validate.</param>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    /// <returns>
    ///     <see langword="true" /> if the <see cref="QueueableTask" /> is ready to be executed, <see langword="false" />
    ///     otherwise.
    /// </returns>
    [UsedImplicitly]
    protected virtual async Task<bool> IsTaskValid(QueueableTask task, CancellationToken token)
    {
        if (task.IsCancelled)
            return false;

        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (task.UnixTimeStampToExecute <= currentUnixTimestamp)
            return true;

        if (!Queue.TryPeek(out _))
            await Task.Delay(DelayOnItemNotReadyAndSolo, token);

        Queue.Enqueue(task);
        return false;
    }

    /// <summary>
    ///     Executes the <see cref="QueueableTask" /> without throwing an <see cref="Exception" /> and instead calling
    ///     <see cref="ExceptionRaised" /> if this is the case.
    /// </summary>
    /// <param name="task">The <see cref="QueueableTask" /> to be executed.</param>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    [UsedImplicitly]
    protected virtual async Task ExecuteTask(QueueableTask task, CancellationToken token)
    {
        try
        {
            await task.ExecuteWithCancelCheck(token);
        }
        catch (Exception e)
        {
            ExceptionRaised(e);
        }
    }

    /// <summary>
    ///     Checks if the <see cref="QueueableTask" /> is a repeating task, and if so, places it back in the queue.
    /// </summary>
    /// <param name="task">The <see cref="QueueableTask" /> to check.</param>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    [UsedImplicitly]
    protected virtual Task CheckAndQueueRepeatingTask(QueueableTask task, CancellationToken token)
    {
        if (!task.IsRepeating || task.IsCancelled)
            return Task.CompletedTask;

        task.ResetTimestamp();
        Queue.Enqueue(task);
        return Task.CompletedTask;
    }
}