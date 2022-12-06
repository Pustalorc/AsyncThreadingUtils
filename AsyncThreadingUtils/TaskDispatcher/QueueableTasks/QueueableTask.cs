using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.TaskDispatcher.QueueableTasks;

public abstract class QueueableTask
{
    protected long Delay { get; set; }

    public long UnixTimeStampToExecute { get; protected set; }
    public bool IsRepeating { get; protected set; }
    public bool IsCancelled { get; protected set; }

    protected QueueableTask(long initialDelay = 0)
    {
        Delay = initialDelay;
        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        UnixTimeStampToExecute = currentUnixTimestamp + Delay;
    }

    public virtual void Cancel()
    {
        IsCancelled = true;
    }

    public virtual async Task ExecuteWithCancelCheck(CancellationToken token)
    {
        if (IsCancelled || token.IsCancellationRequested)
            return;

        await Execute(token);
    }

    public virtual void ResetTimestamp()
    {
        var currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        UnixTimeStampToExecute = currentUnixTimestamp + Delay;
    }

    protected abstract Task Execute(CancellationToken token);
}