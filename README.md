# Culbertson

> Minimal reproduction project for a `System.CommandLine` termination handling issue.  

## Overview

This repo demonstrates a bug introduced in [`System.CommandLine v2.0.0-beta7.25380.108`](https://github.com/dotnet/command-line-api).  The last commit _without_ this issue was [`7bb08a6`](https://github.com/dotnet/command-line-api/commit/7bb08a6038dfc3faecfbacf7c2a9136d1638e77b).  

- In commit [`3132acb`](https://github.com/dotnet/command-line-api/commit/3132acb152db3f00f71d00a9baf93dee12efe771), `CommandLineConfiguration` was renamed to `InvocationConfiguration`.
- During that change, the default value for `ProcessTerminationTimeout` was dropped.
- Without a default, the following line is skipped in `InvocationPipeline`:

  ```csharp
  await terminationHandler.Start(processTerminationTimeout, cancellationToken);
  ```

- This causes the subsequent call to `Task.WhenAny(...)` to be skipped.  As a result OperationCanceledException may not be propogated to the program consuming System.CommandLine.

## Repro Steps

1. Clone the repo.
2. Build with the .NET SDK 9.0+:

   ```sh
   dotnet build ./src/Culbertson
   ```

3. Run the app

   ```sh
   dotnet run --project ./src/Culbertson
   ```

4. Press Ctrl+C.

## Expected behavior

The app should,

- Catch the OperationCanceledException
- Print cleanup messages
- Exit with code 42.

## Actual behavior (with current System.CommandLine 2.0.0-beta7.25380.108)

The app exits, but does not handle the OperationCanceledException.  This flaw jeopardizes potential cleanup routines for consumers.
