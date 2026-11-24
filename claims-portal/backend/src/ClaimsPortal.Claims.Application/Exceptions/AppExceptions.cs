namespace ClaimsPortal.Claims.Application.Exceptions;

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message)
    {
    }
}

public sealed class ForbiddenAppException : Exception
{
    public ForbiddenAppException(string message) : base(message)
    {
    }
}

public sealed class NotFoundAppException : Exception
{
    public NotFoundAppException(string message) : base(message)
    {
    }
}
