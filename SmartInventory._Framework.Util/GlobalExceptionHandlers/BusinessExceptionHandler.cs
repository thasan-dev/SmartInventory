using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartInventory._Framework.Util.Exceptions.BusinessExceptions;

namespace SmartInventory._Framework.Util.Exceptions.GlobalExceptionHandlers;

public class BusinessExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BusinessExceptionHandler> _logger;

    public BusinessExceptionHandler(ILogger<BusinessExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessException ex)
            return false;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogWarning(exception,
            "Domain error: {ErrorCode} | TraceId={TraceId}",
            400, traceId);

        var details = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "A business rule was violated.",
            Detail = ex.Message,
            Type = $"https://errors.yourdomain.com/{400}",
            Instance = context.Request.Path
        };

        details.Extensions["traceId"] = traceId;
        details.Extensions["code"] = 422;

        context.Response.StatusCode = details.Status.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(details, cancellationToken);
        return true;
    }
}

