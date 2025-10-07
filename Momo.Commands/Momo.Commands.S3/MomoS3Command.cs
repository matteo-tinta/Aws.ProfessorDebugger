using Momo.Commands.S3.Publish;

namespace Momo.Commands.S3;

public class MomoS3Command : IMomoCommand
{
    private readonly MomoS3CommandType _type;
    private readonly MomoS3CommandDto _dto;
    private readonly IMomoCommand _command;

    public MomoS3Command(MomoS3CommandType type, MomoS3CommandDto dto)
    {
        _type = type;
        _dto = dto;
        _command = GetCommandByType();
    }
    
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var command = GetCommandByType();
        return command.ExecuteAsync(cancellationToken);
    }

    private IMomoCommand GetCommandByType() => _type switch
    {
        MomoS3CommandType.Publish => new MomoPublishCommand(_dto.PublishOptions ?? throw new InvalidOperationException("Publish options are invalid")),
        _ => throw new InvalidDataException($"Unknown S3 Operation: {_type}")
    };

    public override string ToString() => $"S3 -> {_command}";
}