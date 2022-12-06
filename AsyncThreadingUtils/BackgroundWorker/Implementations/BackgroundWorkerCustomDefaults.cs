using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Implementations;

/// <inheritdoc />
/// <summary>
///     A background worker that allows for construction of a worker with custom defaults.
/// </summary>
/// <remarks>
///     The default initialized objects are required here.
/// </remarks>
[UsedImplicitly]
public sealed class BackgroundWorkerCustomDefaults : BackgroundWorkerWithTasks
{
    /// <inheritdoc />
    /// <summary>
    ///     The constructor for this class.
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
    ///     <see cref="BackgroundWorkerWithTasks.Start" /> is called and the worker is busy
    /// </param>
    /// <param name="stopThrowsExceptions">
    ///     If the background worker should throw an exception when
    ///     <see cref="BackgroundWorkerWithTasks.Stop" /> or <see cref="BackgroundWorkerWithTasks.NonBlockingStop" /> is called
    ///     and the worker is not currently executing
    /// </param>
    [UsedImplicitly]
    public BackgroundWorkerCustomDefaults(Func<CancellationToken, Task> functionToExecute,
        Action<Exception> exceptionRaised, bool repeating = true, bool startThrowsExceptions = true,
        bool stopThrowsExceptions = true)
    {
        StartThrowsExceptions = startThrowsExceptions;
        StopThrowsExceptions = stopThrowsExceptions;
        Repeating = repeating;
        ExceptionRaised = exceptionRaised;
        FunctionToExecute = functionToExecute;
    }
}