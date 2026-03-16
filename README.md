# vandelpal.commands

`vandelpal.commands` is a lightweight command pipeline.
It helps you compose app flows (init, loading, staged tasks) from small commands with:

- sequential execution (`QueueCommand`)
- parallel execution (`BoxCommand`)
- progress aggregation (`ProgressQueueCommand`)
- timeout, retry, completion/error handling
- sync and async steps (`ActionWrapperCommand`, `WaitSecondsCommand`, `ExecuteAsync`)

## When To Use

Use this package when you want predictable orchestration:

- app/bootstrap startup flows
- loading pipelines with weighted progress
- feature initialization with explicit fail behavior
- manual step-by-step execution for gated flows

## Requirements

- Unity 2021.3+
- [UniTask](https://github.com/Cysharp/UniTask)

## Test Dependencies

- [NSubstitute](https://github.com/nsubstitute/NSubstitute) (used in `Tests`)
- NUnit (Unity Test Framework)

## Quick Setup

1. Add package `com.vandelpal.commands`.
2. Provide:
   - `ICommandLogger` for logs
   - `ICommandBugTracker` for bug payload reporting
3. Create your own commands and execute them.

## Minimal Example

Create a command:

```csharp
public class TestCommand : AbstractCommand {
    private readonly object _context;

    public TestCommand(object context, ICommandLogger logger, ICommandBugTracker bugTracker)
        : base(logger, bugTracker) {
        _context = context;
    }

    protected override void ExecInternal() {
        // Some business logic
        NotifyComplete();
    }
}
```

Execute it:

```csharp
var logger = new UnityCommandLogger();
var bugTracker = new UnityCommandBugTracker();

var cmd = new TestCommand(new object(), logger, bugTracker);
cmd.AddCompleteHandler(c => Debug.Log("Command completed"));
cmd.Execute();
```

Use a queue for multiple commands:

```csharp
var factory = new CommandsFactory(logger, bugTracker);

var queue = factory.GetQueueCommand("InitFlow", CommandFailBehaviour.Continue);
queue.Add(new LoadConfigCommand(logger, bugTracker));
queue.Add(new ConnectBackendCommand(logger, bugTracker));
queue.Add(new WarmupCacheCommand(logger, bugTracker));
queue.AddCompleteHandler(cmd => Debug.Log($"Done: {cmd.IsSucceed}"));
queue.Execute();
```

`LoadConfigCommand`, `ConnectBackendCommand`, `WarmupCacheCommand` are your domain commands
(typically inherited from `AbstractCommand` / `AbstractProgressCommand`).

## Core Building Blocks

- `ICommand`: base contract (`Execute`, `ExecuteAsync`, `Retry`, `Terminate`, state/time/error)
- `QueueCommand`: sequential pipeline (continue or terminate on failed step)
- `BoxCommand`: parallel command group with shared completion/progress
- `ProgressQueueCommand`: sequential queue with weighted progress and optional fake progress
- `CommandsFactory`: convenient creation with shared logger/bug tracker

## Practical Patterns

- **Manual queue mode**: set `QueueExecuteMode.Manual` and move to next step with `ContinueExecute()`.
- **Combined loading**: run multiple progress queues in one `BoxCommand` with weights.
- **Async integration**: use `ExecuteAsync()` and `UniTask.WhenAll(...)` for command-level await.
- **Wrappers**: use `ActionWrapperCommand`, `WaitSecondsCommand`, `PredicateWrapperCommand`, `NotWaitWrapperCommand` to compose flows quickly.

## Samples

See `Samples~/BasicUsage`:

- `BasicUsageExample` for quick entry points
- `AdvancedUsageExample` for combined queues, manual mode, wrappers, and async scenarios

## Tests

Tests live in `Tests/` (NUnit + NSubstitute).
