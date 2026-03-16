# Changelog

All notable changes to this package are documented in this file.

## [1.0.0] - 2026-03-14

- Initial public release of `vandelpal.commands`.
- Added command abstractions (`ICommand`, `IProgressCommand`, queue/box variants).
- Added queue orchestration (`QueueCommand`, `ProgressQueueCommand`, `BoxCommand`).
- Added wrappers (`ActionWrapperCommand`, `WaitSecondsCommand`, `PredicateWrapperCommand`, `NotWaitWrapperCommand`).
- Added timeout handling, logging, bug tracking integration, and sample usage.
