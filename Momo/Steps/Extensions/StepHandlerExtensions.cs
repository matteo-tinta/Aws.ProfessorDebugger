using Momo.Steps.Decorations;

namespace Momo.Steps.Extensions;

public static class StepHandlerExtensions
{
    /// <summary>
    /// Adds a wrapper that logs operations in console
    /// </summary>
    /// <param name="handler">The main handler</param>
    /// <param name="options">The client factory options</param>
    /// <returns></returns>
    public static IStepHandler DecorateWithLogging(this IStepHandler handler, MomoClientFactoryOptions options)
    {
        if (handler is StepHandlerLoggingDecorated)
        {
            return handler; //do nothing, is already wrapped
        }
        
        return new StepHandlerLoggingDecorated(handler, options);
    }
}