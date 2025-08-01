namespace AccountService.Shared.Exceptions;

public class OperationNotAllowedException(string message) : Exception(message);