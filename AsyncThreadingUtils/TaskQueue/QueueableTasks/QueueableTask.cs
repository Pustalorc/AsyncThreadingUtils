using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.Implementations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.QueueableTasks;

/// <summary>
///     An abstract class that allows a user to define a custom Queueable Task for the <see cref="TaskQueue" />.
/// </summary>
public abstract class QueueableTask
{
    /// <summary>
    ///     The delay (in milliseconds) before this task should be executed.
    /// </summary>
    [UsedImplicitly]
    protected long Delay { get; set; }

    /// <summary>
    ///     The Unix Timestamp at which this task should execute.
    /// </summary>
    [UsedImplicitly]
    public long UnixTimeStampToExecute { get; protected set; }

    /// <summary>
    ///     A boolean determining if this task should repeat after execution.
    /// </summary>
    [UsedImplicitly]
    public bool IsRepeating { get; protected set; }

    /// <summary>
    ///     A boolean determining if this task has been cancelled.
    /// </summary>
    [UsedImplicitly]
    public bool IsCancelled { get; protected set; }

    /// <summary>
    ///     The constructor for all queueable tasks.
    /// </summary>
    /// <param name="initialDelay">The initial delay (in milliseconds) before the task executes. Defaults to 0</param>
    protected QueueableTask(long initialDelay = 0)
    {
        Delay = initialDelay;
        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UnixTimeStampToExecute = currentUnixTimestamp + Delay;
    }

    /// <summary>
    ///     Cancels this task and prevents it from executing further.
    /// </summary>
    [UsedImplicitly]
    public virtual void Cancel()
    {
        IsCancelled = true;
    }

    /// <summary>
    ///     Provides an entrypoint for execution that checks if the task is cancelled before finalizing the execution.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> in order to cancel this task.</param>
    [UsedImplicitly]
    public virtual async Task ExecuteWithCancelCheck(CancellationToken token)
    {
        if (IsCancelled || token.IsCancellationRequested)
            return;

        await Execute(token);
    }

    /// <summary>
    ///     Resets the <see cref="UnixTimeStampToExecute" /> with <see cref="Delay" /> in order to get this task to execute in
    ///     the future again.
    /// </summary>
    [UsedImplicitly]
    public virtual void ResetTimestamp()
    {
        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UnixTimeStampToExecute = currentUnixTimestamp + Delay;
    }

    /// <summary>
    ///     The method with the work that is queued to be done.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> in order to cancel this task.</param>
    protected abstract Task Execute(CancellationToken token);
}