using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker;

public sealed class BackgroundWorkerCustomDefaults : BackgroundWorkerWithTasks
{
    public BackgroundWorkerCustomDefaults(Func<CancellationToken, Task> functionToExecute, Action<Exception> exceptionRaised, bool repeating = true)
    {
        Repeating = repeating;
        ExceptionRaised = exceptionRaised;
        FunctionToExecute = functionToExecute;
    }
}