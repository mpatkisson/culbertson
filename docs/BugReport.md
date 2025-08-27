# OperationCanceledException Not Thrown without Config in Async Apps

[This documentation (published 02 AUG 25) on Learn](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-parse-and-invoke) implies that `System.CommandLine` will, without additional configuration, throw `OperationCanceledException` when `CTRL+C` is pressed allowing library consumers to gracefully handle any necessary cleanup or messaging to end users.

Versions [2.0.0-beta5](https://github.com/dotnet/command-line-api/commits/v2.0.0-beta5.25306.1) and [2.0.0-beta6](https://github.com/dotnet/command-line-api/commits/v2.0.0-beta6.25358.103) both supported this behavior, [but 2.0.0-beta7](https://github.com/dotnet/command-line-api/commits/v2.0.0-beta7.25380.108) and [beyond](https://github.com/dotnet/command-line-api/commits/main) break it.

## Steps to reproduce

1. Clone and run <https://github.com/mpatkisson/culbertson> with .NET 9

   ```sh
   > git clone https://github.com/mpatkisson/culbertson.git
   > cd culbertson
   > dotnet run --project ./src/Culbertson
   ```

2. Once the project is running, press `CTRL+C`.

### Expected Behavior

[This routine](https://github.com/mpatkisson/culbertson/blob/c0571eaf82ba6b36738ac32ab952b5086af55c9b/src/Culbertson/Program.cs#L13) should be handled when `CTRL+C` is pressed, thus printing these message along with a slight delay for effect,

```sh
Termination requested. Cleaning up...
# 1 second delay
Cleanup complete. Exiting.
```

### Actual Behavior

The program _does_ exit when `CTRL+C` is pressed, but `OperationCanceledException` is never thrown, so cleanup messaging is _not_ displayed.

## Further Analysis

My guess is `OperationCanceledException` _should_ be thrown in Beta 7 as it is in Betas 5 and 6.  If this was a design decision, then apologies (but the docs / code commentary are confusing if so).

[`7bb08a6`](https://github.com/dotnet/command-line-api/commit/7bb08a6038dfc3faecfbacf7c2a9136d1638e77b) was the latest commit supporting the expected behavior.  `7bb08a6` was followed by [`3132acb | Separate parse from invocation configurations (#2606)`](https://github.com/dotnet/command-line-api/commit/3132acb152db3f00f71d00a9baf93dee12efe771) which had a lot of changes.

In the `InvocationPipeline`, [this switch case](https://github.com/dotnet/command-line-api/blob/4494d98feedcca2b68177236da02b940b04d2fa3/src/System.CommandLine/Invocation/InvocationPipeline.cs#L53) creates a `ProcessTerminationHandler` _only if_ `InvocationConfiguration.ProcessTerminationTimeout` is non-null.  Before Beta7 a default value of 2s [was being set on `CommandLineConfiguration.ProcessTerminationTimeout`](https://github.com/dotnet/command-line-api/blob/7bb08a6038dfc3faecfbacf7c2a9136d1638e77b/src/System.CommandLine/CommandLineConfiguration.cs#L69) like so

```csharp
public TimeSpan? ProcessTerminationTimeout { get; set; } = TimeSpan.FromSeconds(2);
```

Commit `3132acb | Separate parse from invocation configurations (#2606)` renamed `CommandLineConfiguration` to `InvocationConfiguration` and [_dropped_ the default setting](https://github.com/dotnet/command-line-api/blob/3132acb152db3f00f71d00a9baf93dee12efe771/src/System.CommandLine/InvocationConfiguration.cs#L21) which now looks like this.

```csharp
/// <summary>
/// Enables signaling and handling of process termination (Ctrl+C, SIGINT, SIGTERM) via a <see cref="CancellationToken"/> 
/// that can be passed to a <see cref="CommandLineAction"/> during invocation.
/// If not provided, a default timeout of 2 seconds is enforced.
///                  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
/// </summary>
public TimeSpan? ProcessTerminationTimeout { get; set; }
```

## Possible Fix

My sense is that the default value on `InvocationConfiguration.ProcessTerminationTimeout` got lost in the mix of [3132acb](https://github.com/dotnet/command-line-api/commit/3132acb152db3f00f71d00a9baf93dee12efe771) and is [still not fixed in the latest](https://github.com/dotnet/command-line-api/blob/4494d98feedcca2b68177236da02b940b04d2fa3/src/System.CommandLine/InvocationConfiguration.cs#L21).

Re-adding the default brings behavior around `CTRL+C` back in line with earlier versions and the Learn docs.  I've tested this small change using [this branch](https://github.com/mpatkisson/command-line-api/tree/resolve-operationcanceledexception-not-thrown-without-config-in-async-apps) with success.
