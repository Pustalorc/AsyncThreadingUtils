using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker;

public class BackgroundWorkerWithTasks
{
    public virtual event WorkerCompleted? WorkerExecutionCompleted;

    public virtual bool IsCompleted => ExecutingTask.IsCompleted;
    public virtual bool IsBusy => !ExecutingTask.IsCompleted;

    public bool Repeating { get; set; }
    public Action<Exception> ExceptionRaised { get; set; }
    public Func<CancellationToken, Task> FunctionToExecute { get; set; }

    protected CancellationTokenSource Cts { get; set; }
    protected Task ExecutingTask { get; set; }

    protected BackgroundWorkerWithTasks()
    {
        Cts = new CancellationTokenSource();
        Repeating = true;
        ExecutingTask = Task.CompletedTask;
        ExceptionRaised = Console.WriteLine;
        FunctionToExecute = _ => Task.CompletedTask;
    }

    public virtual void Start()
    {
        if (IsBusy)
            return;

        Cts = new CancellationTokenSource();
        ExecutingTask = Task.Run(ExecuteWork);
    }

    public virtual void Stop()
    {
        if (IsCompleted)
            return;

        Cts.Cancel();
        ExecutingTask.Wait();
    }

    public virtual void NonBlockingStop()
    {
        if (IsCompleted)
            return;

        Cts.Cancel();
    }

    protected virtual async Task ExecuteWork()
    {
        do
        {
            try
            {
                await FunctionToExecute(Cts.Token);
            }
            catch (Exception exception)
            {
                if (exception is TaskCanceledException)
                    continue;

                ExceptionRaised(exception);
            }
        } while (!Cts.IsCancellationRequested && Repeating);

        WorkerExecutionCompleted?.Invoke(this);
    }
}