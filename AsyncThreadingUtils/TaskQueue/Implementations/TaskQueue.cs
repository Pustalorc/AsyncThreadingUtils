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
[PublicAPI]
public class TaskQueue
{
    /// <summary>
    ///     The <see cref="BackgroundWorkerWithTasks" /> that will execute the <see cref="QueueableTask" />s in the
    ///     <see cref="Queue" />.
    /// </summary>
    protected BackgroundWorkerWithTasks BackgroundWorker { get; }

    /// <summary>
    ///     The <see cref="ConcurrentQueue{T}" /> for the class.
    /// </summary>
    /// <remarks>
    ///     This is concurrent as we will be queueing and dequeueing on different threads, avoiding the use of
    ///     <see langword="lock" />s.
    /// </remarks>
    protected ConcurrentQueue<QueueableTask> Queue { get; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the queue currently be empty.
    /// </summary>
    public int DelayOnEmptyQueue { get; set; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the current item not be ready for execution.
    /// </summary>
    public int DelayOnItemNotReady { get; set; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the entire queue not be ready for execution.
    /// </summary>
    public int DelayOnQueueNotReady { get; set; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, after an item has finished its own execution.
    /// </summary>
    public int DelayOnItemExecuted { get; set; }

    /// <summary>
    ///     The delay (in milliseconds) before execution should resume, should the current item not be ready for execution and
    ///     be the only item in the queue.
    /// </summary>
    [Obsolete("Replaced by DelayOnQueueNotReady. Same functionality is provided, but is more exhaustive.", true)]
    public int DelayOnItemNotReadyAndSolo
    {
        get => DelayOnQueueNotReady;
        set => DelayOnQueueNotReady = value;
    }

    /// <summary>
    ///     Gets or sets the <see cref="Action{T}" /> that will be called when an <see cref="Exception" /> is raised.
    /// </summary>
    /// <remarks>
    ///     This property gets and sets the value directly from the underlying BackgroundWorker.
    /// </remarks>
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
    public bool StopThrowsExceptions
    {
        get => BackgroundWorker.StopThrowsExceptions;
        set => BackgroundWorker.StopThrowsExceptions = value;
    }

    /// <summary>
    ///     Sets if the current queue has been explored. Used by IsTaskValid to apply the correct delay if it has been
    ///     explored.
    /// </summary>
    protected bool QueueExplored { get; set; }

    /// <summary>
    ///     Sets the first task from the queue when exploring from scratch. Used by IsTaskValid to set the value of
    ///     QueueExplored.
    /// </summary>
    protected QueueableTask? FirstNonReadyTask { get; set; }

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
        DelayOnQueueNotReady = 1000;
        DelayOnItemNotReady = 25;
        DelayOnItemExecuted = 1;
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
    [Obsolete("New constructor with more parameter options added. Please change to use that one.", true)]
    public TaskQueue(Action<Exception> exceptionRaised, int delayOnEmptyQueue = 5000,
        int delayOnItemNotReadyAndSolo = 1000,
        bool startThrowsExceptions = true, bool stopThrowsExceptions = true)
    {
        Queue = new ConcurrentQueue<QueueableTask>();

        BackgroundWorker = new BackgroundWorkerWithTasks(ExecuteWork, exceptionRaised, true, startThrowsExceptions,
            stopThrowsExceptions);

        DelayOnEmptyQueue = delayOnEmptyQueue;
        DelayOnQueueNotReady = delayOnItemNotReadyAndSolo;
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
    /// <param name="delayOnQueueNotReady">
    ///     The delay (in milliseconds) before execution should resume, should the entire queue not be ready for execution.
    /// </param>
    /// <param name="delayOnItemNotReady">
    ///     The delay (in milliseconds) before execution should resume, should the current item
    ///     not be ready for execution.
    /// </param>
    /// <param name="delayOnItemExecuted">
    ///     The delay (in milliseconds) before execution should resume, after an item has finished its own execution.
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
        int delayOnQueueNotReady = 1000, int delayOnItemNotReady = 25, int delayOnItemExecuted = 1,
        bool startThrowsExceptions = true, bool stopThrowsExceptions = true)
    {
        Queue = new ConcurrentQueue<QueueableTask>();

        BackgroundWorker = new BackgroundWorkerWithTasks(ExecuteWork, exceptionRaised, true, startThrowsExceptions,
            stopThrowsExceptions);

        DelayOnEmptyQueue = delayOnEmptyQueue;
        DelayOnQueueNotReady = delayOnQueueNotReady;
        DelayOnItemNotReady = delayOnItemNotReady;
        DelayOnItemExecuted = delayOnItemExecuted;
    }

    /// <summary>
    ///     Starts the processing of the queue for execution.
    /// </summary>
    /// <remarks>
    ///     This does not reset or clear the queue.
    /// </remarks>
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
    public virtual void QueueTask(QueueableTask task)
    {
        Queue.Enqueue(task);

        if (QueueExplored)
            QueueExplored = false;

        if (FirstNonReadyTask != null)
            FirstNonReadyTask = null;
    }

    /// <summary>
    ///     Executes work from the queue.
    /// </summary>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
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
        await Task.Delay(DelayOnItemExecuted, token);
    }

    /// <summary>
    ///     Retrieves the next task to be executed from the queue.
    /// </summary>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
    /// <returns>A <see cref="QueueableTask" /> if there was one in the queue, <see langword="null" /> otherwise.</returns>
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
    protected virtual async Task<bool> IsTaskValid(QueueableTask task, CancellationToken token)
    {
        if (task.IsCancelled)
            return false;

        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (task.UnixTimeStampToExecute <= currentUnixTimestamp)
            return true;

        var delay = DelayOnItemNotReady;

        if (QueueExplored)
            delay = DelayOnQueueNotReady;
        else if (FirstNonReadyTask == null)
            FirstNonReadyTask = task;
        else if (FirstNonReadyTask == task)
            QueueExplored = true;

        await Task.Delay(delay, token);
        Queue.Enqueue(task);
        return false;
    }

    /// <summary>
    ///     Executes the <see cref="QueueableTask" /> without throwing an <see cref="Exception" /> and instead calling
    ///     <see cref="ExceptionRaised" /> if this is the case.
    /// </summary>
    /// <param name="task">The <see cref="QueueableTask" /> to be executed.</param>
    /// <param name="token">The <see cref="CancellationToken" /> to dynamically cancel execution when requested.</param>
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
    protected virtual Task CheckAndQueueRepeatingTask(QueueableTask task, CancellationToken token)
    {
        if (!task.IsRepeating || task.IsCancelled)
            return Task.CompletedTask;

        task.ResetTimestamp();
        Queue.Enqueue(task);
        return Task.CompletedTask;
    }
}