using System;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Exceptions;

/// <inheritdoc />
/// <summary>
///     Represents an error when a background worker was attempted to be stopped whilst it was already stopped.
/// </summary>
[UsedImplicitly]
public class BackgroundWorkerNotRunningException : Exception
{
    /// <inheritdoc />
    /// <summary>
    ///     Constructs the exception with the default message.
    /// </summary>
    [UsedImplicitly]
    public BackgroundWorkerNotRunningException() : base(
        "Background worker cannot be stopped as it is not currently in execution.")
    {
    }

    /// <inheritdoc />
    /// <summary>
    ///     Constructs the exception with a custom message.
    /// </summary>
    [UsedImplicitly]
    public BackgroundWorkerNotRunningException(string message) : base(message)
    {
    }
}