using Momo.Commands;
using Momo.Models;
using Tests.Shared.Builders;

namespace Momo.Tests.Test.Extensions;

internal static class ModelBuilderMomoExpectationFileTestExtensions
{
    internal static MomoClientCommand WithCommands(this ModelBuilder<MomoExpectationFile> builder, IMomoCommand command,
        IMomoCommand? undoCommand = null)
    {
        var momoCommand = new MomoClientCommand()
        {
            Command = command,
            Undo = undoCommand
        };
        
        builder.Set(c => c.Commands, [ momoCommand ]);

        return momoCommand;
    }
}