namespace LandsatReflectance.UI.Exceptions.Api;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    { }
}