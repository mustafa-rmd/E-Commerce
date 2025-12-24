namespace E_Commerce.API.Domain.Exceptions;

public class InvalidOrderStateTransitionException : DomainException
{
    public InvalidOrderStateTransitionException(string message) : base(message)
    {
    }

    public InvalidOrderStateTransitionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
