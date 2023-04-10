using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Delegates;
using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Exceptions;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Implementations;

/// <summary>
///     A background worker that supports methods or functions returning a Task for asynchronous support.
/// </summary>
/// <remarks>
///     This object is initialized with some empty defaults.
///     For it to do things, please set values for the public properties.
/// </remarks>
[PublicAPI]
public class BackgroundWorkerWithTasks
{
    /// <summary>
    ///     An event that is raised once execution of the worker has completed.
    /// </summary>
    /// <remarks>
    ///     You should not use this event if you plan on restarting execution from this.
    ///     This event is raised within the running task, which will prevent Start() from running a new task.
    /// </remarks>
    public virtual event WorkerCompleted? WorkerExecutionCompleted;

    /// <summary>
    ///     A boolean that will return <see langword="true" /> or <see langword="false" /> depending on if the executing task
    ///     has completed.
    /// </summary>
    public virtual bool IsCompleted => ExecutingTask.IsCompleted;

    /// <summary>
    ///     A boolean that will return <see langword="true" /> or <see langword="false" /> depending on if the executing task
    ///     is currently running.
    /// </summary>
    public virtual bool IsBusy => !ExecutingTask.IsCompleted;

    /// <summary>
    ///     If this is set to <see langword="true" />, calling <see cref="Start" /> when the worker is busy will cause an
    ///     exception to be raised.
    /// </summary>
    public bool StartThrowsExceptions { get; set; }

    /// <summary>
    ///     If this is set to <see langword="true" />, calling <see cref="Stop" /> or <see cref="NonBlockingStop" /> when the
    ///     worker is not executing will cause an exception to be raised.
    /// </summary>
    public bool StopThrowsExceptions { get; set; }

    /// <summary>
    ///     If this is set to <see langword="true" />, then the execution will continuously loop until the worker is manually
    ///     stopped by calling <see cref="Stop" /> or <see cref="NonBlockingStop" />.
    /// </summary>
    public bool Repeating { get; set; }

    /// <summary>
    ///     An <see cref="Action{T}" /> that will be called when an <see cref="Exception" /> is raised. Defaults to writing a
    ///     new line in console.
    /// </summary>
    public Action<Exception> ExceptionRaised { get; set; }

    /// <summary>
    ///     A <see cref="Func{T,TResult}" /> that will be called when the background worker is executing.
    ///     If <see cref="Repeating" /> is set to true, this will be called repeatedly.
    /// </summary>
    public Func<CancellationToken, Task> FunctionToExecute { get; set; }

    /// <summary>
    ///     The <see cref="CancellationTokenSource" /> to add cancellation and stopping support.
    ///     Asynchronous tasks should support cancellation with the token provided by the
    ///     <see cref="CancellationTokenSource" />.
    /// </summary>
    protected CancellationTokenSource CancellationTokenSource { get; set; }

    /// <summary>
    ///     The currently stored <see cref="Task" /> that is doing the work of this background worker.
    ///     This <see cref="Task" /> should be running on a separate thread.
    /// </summary>
    protected Task ExecutingTask { get; set; }

    /// <summary>
    ///     The default constructor for this class. It will default all values to non-null, and exceptions will be thrown by
    ///     default.
    /// </summary>
    public BackgroundWorkerWithTasks()
    {
        CancellationTokenSource = new CancellationTokenSource();
        ExecutingTask = Task.CompletedTask;

        FunctionToExecute = _ => Task.CompletedTask;
        ExceptionRaised = Console.WriteLine;
        Repeating = false;
        StartThrowsExceptions = true;
        StopThrowsExceptions = true;
    }

    /// <summary>
    ///     The primary constructor for this class.
    ///     It will allow creating a background worker with the correct required property values.
    /// </summary>
    /// <param name="functionToExecute">The <see cref="Func{T,TResult}" /> that the background worker will execute.</param>
    /// <param name="exceptionRaised">
    ///     The <see cref="Action{T}" /> that will be called if an exception happens in the
    ///     background worker.
    /// </param>
    /// <param name="repeating">If the background worker should repeat execution indefinitely.</param>
    /// <param name="startThrowsExceptions">
    ///     If the background worker should throw an exception when
    ///     <see cref="Start" /> is called and the worker is busy
    /// </param>
    /// <param name="stopThrowsExceptions">
    ///     If the background worker should throw an exception when
    ///     <see cref="Stop" /> or <see cref="NonBlockingStop" /> is called
    ///     and the worker is not currently executing
    /// </param>
    public BackgroundWorkerWithTasks(Func<CancellationToken, Task> functionToExecute, Action<Exception> exceptionRaised,
        bool repeating = false, bool startThrowsExceptions = true, bool stopThrowsExceptions = true)
    {
        CancellationTokenSource = new CancellationTokenSource();
        ExecutingTask = Task.CompletedTask;

        FunctionToExecute = functionToExecute;
        ExceptionRaised = exceptionRaised;
        Repeating = repeating;
        StartThrowsExceptions = startThrowsExceptions;
        StopThrowsExceptions = stopThrowsExceptions;
    }

    /// <summary>
    ///     Starts the background worker for execution.
    /// </summary>
    /// <exception cref="BackgroundWorkerBusyException">
    ///     An exception that is raised if you attempt to start the background worker whilst it is already executing.
    ///     Only raised if <see cref="StartThrowsExceptions" /> is <see langword="true" />.
    /// </exception>
    /// <remarks>
    ///     The <see cref="CancellationTokenSource" /> should be re-set to default values here.
    /// </remarks>
    public virtual void Start()
    {
        if (IsBusy)
        {
            if (StartThrowsExceptions)
                throw new BackgroundWorkerBusyException();

            return;
        }

        CancellationTokenSource = new CancellationTokenSource();
        ExecutingTask = Task.Run(ExecuteWork);
    }

    /// <summary>
    ///     Stops the background worker's current execution.
    /// </summary>
    /// <exception cref="BackgroundWorkerNotRunningException">
    ///     An exception that is raised if you attempt to stop the background worker when it isn't currently running.
    ///     Only raised if <see cref="StopThrowsExceptions" /> is <see langword="true" />.
    /// </exception>
    /// <remarks>
    ///     The <see cref="CancellationTokenSource" /> should be cancelled here.
    ///     This method is blocking and will wait for <see cref="ExecutingTask" /> to finish execution before returning.
    ///     The cancellation should be quick provided the underlying asynchronous code supports
    ///     <see cref="CancellationToken" />s.
    ///     If you require non-blocking stop, use <see cref="NonBlockingStop" />.
    /// </remarks>
    public virtual void Stop()
    {
        if (IsCompleted)
        {
            if (StopThrowsExceptions)
                throw new BackgroundWorkerNotRunningException();

            return;
        }

        CancellationTokenSource.Cancel();
        ExecutingTask.Wait();
    }

    /// <summary>
    ///     Stops the background worker's current execution, but will not wait for it to complete.
    /// </summary>
    /// <exception cref="BackgroundWorkerNotRunningException">
    ///     An exception that is raised if you attempt to stop the background worker when it isn't currently running.
    ///     Only raised if <see cref="StopThrowsExceptions" /> is <see langword="true" />.
    /// </exception>
    /// <remarks>
    ///     The <see cref="CancellationTokenSource" /> should be cancelled here.
    /// </remarks>
    public virtual void NonBlockingStop()
    {
        if (IsCompleted)
        {
            if (StopThrowsExceptions)
                throw new BackgroundWorkerNotRunningException();

            return;
        }

        CancellationTokenSource.Cancel();
    }

    /// <summary>
    ///     The method responsible for handling the execution that this BackgroundWorker will perform.
    /// </summary>
    protected virtual async Task ExecuteWork()
    {
        do
        {
            try
            {
                await FunctionToExecute(CancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                if (exception is TaskCanceledException)
                    continue;

                ExceptionRaised(exception);
            }
        } while (!CancellationTokenSource.IsCancellationRequested && Repeating);

        WorkerExecutionCompleted?.Invoke(this);
    }
}