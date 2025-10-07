namespace Momo.Commands.Decorations;

public class MomoCommandLoggingDecorated(IMomoCommand command): IMomoCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{GetCommandName()}] Executing command...");

        try
        {
            await command.ExecuteAsync(cancellationToken);
            Console.WriteLine($"[{GetCommandName()}] Command Executed!");
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"[{GetCommandName()}] Command failed: {e.Message}");
            throw; //rethrow
        }
    }

    private string? GetCommandName() => $"Command: {command.ToString() ?? "Unknown"}";
}