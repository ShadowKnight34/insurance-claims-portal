using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace ClaimsPortal.Claims.Api.Infrastructure;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            UnauthorizedAppException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenAppException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundAppException => (StatusCodes.Status404NotFound, "Not found"),
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            InvalidClaimTransitionException => (StatusCodes.Status400BadRequest, "Invalid status transition"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var detail = status == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        await Results.Problem(title: title, detail: detail, statusCode: status).ExecuteAsync(httpContext);
        return true;
    }
}
