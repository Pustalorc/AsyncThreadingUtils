# Async Threading Utils [![NuGet](https://img.shields.io/nuget/v/Pustalorc.AsyncThreadingUtils.svg)](https://www.nuget.org/packages/Pustalorc.AsyncThreadingUtils/)

Library that adds a BackgroundWorker and TaskQueue with Async support.

# Installation & Usage

Before you begin, please make sure that your current solution has installed the nuget package of this library.  
You can find this package [here.](https://www.nuget.org/packages/Pustalorc.AsyncThreadingUtils)

---

## Background Worker Usage

There are 2 ways to create the background worker.  
You can use object initialization on `BackgroundWorkerWithTasks` as follows:

```cs
Worker = new BackgroundWorkerWithTasks
{
    FunctionToExecute = ExecuteWork,
    ExceptionRaised = ExceptionRaised,
    Repeating = true,
    StartThrowsExceptions = false,
    StopThrowsExceptions = false
};
```

Or use the constructor which takes all the same options as parameters:
```cs
Worker = new BackgroundWorkerWithTasks(ExecuteWork, ExceptionRaised, true, false, false);
```

From here, simply start the worker with `Worker.Start()` when you want it to start doing work, and the `ExecuteWork` method will be executed on a separate thread.

If later you wish to stop a repeating worker, or a worker that is doing heavy work in the middle of execution, simply call `Worker.Stop()` for it to stop and the code to wait for it to fully finalize, or `Worker.NonBlockingStop()` to not wait for the worker to fully stop.

If you use NonBlockingStop and still wish to know when exactly the worker has finished, the event `Worker.WorkerExecutionCompleted` is available

---

## Task Queue Usage

Just like the background worker, the task queue can be created in 2 ways.  
You can use object initialization on `TaskQueue` as follows:

```cs
Queue = new TaskQueue
{
    ExceptionRaised = ExceptionRaised,
    DelayOnEmptyQueue = 5000,
    DelayOnItemNotReadyAndSolo = 1000,
    StartThrowsExceptions = false,
    StopThrowsExceptions = false
};
```

Or use the constructor which takes all the same options as parameters:

```cs
Queue = new TaskQueue(ExceptionRaised, 5000, 1000, false, false);
```

From here, simply start the queue with `Queue.Start()` when you want it to start processing the queue.  
Note that the queue will not simply run once and then stop when its empty, it will continuously run, but wait the configured `DelayOnEmptyQueue` before trying again should the queue be empty.  
You can inherit the `TaskQueue` class and modify this behaviour if you so wish.

With the queue running, you can queue tasks to be executed with 2 methods:  
Either `QueueAnonymousTask` which allows to queue lambda expressions or full methods without defining a class for the task, or `QueueTask` which allows to queue specific queueable tasks that you create.

If you wish to see how to create a queueable task, see the `QueuedAnonymousTask` for an example.  
The basics of it however is that an Execute method is implemented that will only run if the task isn't cancelled and its `UnixTimeStampToExecute` has been surpassed by the current unix timestamp (in milliseconds).

You can stop the queue at any time from processing by running `Queue.Stop()` or `Queue.NonBlockingStop()`.  
Both function similarly to the background worker methods.