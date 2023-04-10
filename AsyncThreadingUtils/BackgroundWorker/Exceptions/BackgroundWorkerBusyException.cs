using System;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Exceptions;

/// <inheritdoc />
/// <summary>
///     Represents an error when a background worker was attempted to be started whilst it was already executing.
/// </summary>
[PublicAPI]
public class BackgroundWorkerBusyException : Exception
{
    /// <inheritdoc />
    /// <summary>
    ///     Constructs the exception with the default message.
    /// </summary>
    public BackgroundWorkerBusyException() : base(
        "Background worker cannot be started as it is already executing work.")
    {
    }

    /// <inheritdoc />
    /// <summary>
    ///     Constructs the exception with a custom message.
    /// </summary>
    public BackgroundWorkerBusyException(string message) : base(message)
    {
    }
}