namespace Momo.Exceptions;

public class MessageAssertException: Exception
{
    public string MessageBody { get; private set; }
    
    internal MessageAssertException(string message) : base(message)
    {
            
    }

    internal MessageAssertException(string message, Exception innerException): base(message, innerException)
    {

    }

    public static MessageAssertException CreateExceptionWithMessageBody(string message, string messageBody)
    {
        return new MessageAssertException(message)
        {
            MessageBody = messageBody
        };
    }
    
    public static MessageAssertException CreateExceptionWithMessageBody(string message, string messageBody, Exception innerException)
    {
        return new MessageAssertException(message, innerException)
        {
            MessageBody = messageBody
        };
    }
}