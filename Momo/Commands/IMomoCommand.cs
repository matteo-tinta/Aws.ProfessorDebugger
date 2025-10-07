namespace Momo.Commands;

public interface IMomoCommand
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}