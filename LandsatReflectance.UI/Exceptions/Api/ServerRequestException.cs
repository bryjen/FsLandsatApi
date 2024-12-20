namespace LandsatReflectance.UI.Exceptions.Api;

public class ServerRequestException : Exception
{
    private const string BaseMessage = "An error occurred while processing your request. Please check your internet " +
                                       "connection or try again later. If the problem persists, contact support.";
    
    public ServerRequestException(Exception innerException) : base(BaseMessage, innerException)
    { }
}