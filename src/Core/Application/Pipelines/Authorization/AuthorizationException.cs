namespace Mootable.Application.Pipelines.Authorization;

public sealed class AuthorizationException : Exception
{
    public AuthorizationException() : base("You are not authorized to perform this action.")
    {
    }
    
    public AuthorizationException(string message) : base(message)
    {
    }
}
