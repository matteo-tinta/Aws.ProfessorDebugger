namespace Momo.Exceptions;

public class AssertException: Exception
{
    internal AssertException(string message): base(message)
    {
        
    }

    internal AssertException(string message, Exception innerException): base(message, innerException)
    {
        
    }
}