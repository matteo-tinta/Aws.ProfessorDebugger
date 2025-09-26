namespace Momo.Exceptions;

public class AssertException: Exception
{
    public bool Breakout;
    
    public AssertException(string message): base(message)
    {
        
    }

    public AssertException(string message, Exception innerException): base(message, innerException)
    {
        
    }

    public AssertException BreakWhenRaised()
    {
        Breakout = true;
        return this;
    }
}