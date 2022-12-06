using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskDispatcher.QueueableTasks;

internal sealed class QueuedAnonymousTask : QueueableTask
{
    private Func<CancellationToken, Task> ExecuteMethod { get; }

    public QueuedAnonymousTask(Func<CancellationToken, Task> executeMethod, long delay, bool repeating) : base(delay)
    {
        ExecuteMethod = executeMethod;
        IsRepeating = repeating;
    }

    protected override async Task Execute(CancellationToken token)
    {
        await ExecuteMethod(token);
    }
}