using System.CommandLine;

RootCommand rootCommand = new("Handle termination example");
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    Console.WriteLine("Press Ctrl+C to exit...");
    try
    {
        // Simulate work
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Termination requested. Cleaning up...");
        // Simulate cleanup
        Thread.Sleep(1000); 
        Console.WriteLine("Cleanup complete. Exiting.");
        return 42;
    }
});
await rootCommand.Parse(args).InvokeAsync();
