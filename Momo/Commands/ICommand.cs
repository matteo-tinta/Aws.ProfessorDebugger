namespace Momo.Commands;

internal interface ICommand
{
    Task ExecuteAsync(CancellationToken cancellationToken);
    Task UndoAsync(CancellationToken cancellationToken);
}