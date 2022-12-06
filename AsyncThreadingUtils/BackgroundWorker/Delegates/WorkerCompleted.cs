using Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Implementations;

namespace Pustalorc.Libraries.AsyncThreadingUtils.BackgroundWorker.Delegates;

/// <summary>
///     A delegate for the built in event for background workers when they finish execution completely.
///     Will return the worker so that it's easier to track which worker finished if you use multiple workers.
/// </summary>
public delegate void WorkerCompleted(BackgroundWorkerWithTasks worker);