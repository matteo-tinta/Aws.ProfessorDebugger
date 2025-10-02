namespace Momo.Exceptions;

public class MomoFileValidationException : Exception
{
    public string PropertyName { get; private set; }
    
    public MomoFileValidationException(string propertyName, string message): base(message)
    {
        PropertyName = propertyName;
    }

    public MomoFileValidationException(string propertyName, string message, Exception innerException): base(message, innerException)
    {
        PropertyName = propertyName;
    }
}
public class MomoClientValidationException: Exception
{
    public MomoClientValidationException(string message): base(message)
    {
        
    }
    
    public MomoClientValidationException(string message, Exception innerException): base(message, innerException)
    {
        
    }
}