namespace AccountService.Shared.Exceptions;

public class OperationNotAllowedException : Exception
{
    public OperationNotAllowedException(string message) : base(message)
    {
    }
}