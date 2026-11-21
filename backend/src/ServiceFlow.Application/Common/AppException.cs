namespace ServiceFlow.Application.Common;

public class AppException : Exception
{
    public AppException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Authentication is required.")
        : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "You are not allowed to perform this action.")
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}

/// <summary>
/// Minimal status code constants so Application does not depend on ASP.NET Core.
/// </summary>
file static class StatusCodes
{
    public const int Status409Conflict = 409;
    public const int Status401Unauthorized = 401;
    public const int Status403Forbidden = 403;
}
