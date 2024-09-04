using System.Text.Json;
using Common.Exceptions;

namespace Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        switch (exception)
        {
            case UnauthorizedUserException _:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                break;
            case ArgumentException _:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            case AwsS3PutObjectException _:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            case EntityNotFoundException _:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
        }

        var result = new
        {
            statusCode = context.Response.StatusCode,
            message = exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
}