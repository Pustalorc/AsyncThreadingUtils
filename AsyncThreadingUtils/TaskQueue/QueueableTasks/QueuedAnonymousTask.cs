using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskQueue.QueueableTasks;

/// <inheritdoc />
/// <summary>
///     An anonymous task that has been queued.
/// </summary>
[PublicAPI]
public sealed class QueuedAnonymousTask : QueueableTask
{
    /// <summary>
    ///     The <see cref="Func{T,TResult}" /> that will be executed.
    /// </summary>
    private Func<CancellationToken, Task> FunctionToExecute { get; }

    /// <inheritdoc />
    /// <summary>
    ///     The constructor for anonymous classes.
    /// </summary>
    /// <param name="functionToExecute">The <see cref="T:System.Func`2" /> that will be executed.</param>
    /// <param name="delay">The delay (in milliseconds) before this task should be executed.</param>
    /// <param name="repeating">A boolean determining if this task should repeat after execution.</param>
    public QueuedAnonymousTask(Func<CancellationToken, Task> functionToExecute, long delay, bool repeating) :
        base(delay)
    {
        FunctionToExecute = functionToExecute;
        IsRepeating = repeating;
    }

    /// <inheritdoc />
    protected override async Task Execute(CancellationToken token)
    {
        await FunctionToExecute(token);
    }
}