using System.Text.Json;
using PastebinApp.Api.Models.Responses;
using PastebinApp.Domain.Exceptions;

namespace PastebinApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(exception, "Response has already started, cannot write error response");
            throw exception;
        }

        var (statusCode, response) = MapException(exception);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        _logger.Log(
            statusCode >= 500 ? LogLevel.Error : LogLevel.Information,
            exception,
            "Unhandled exception mapped to HTTP {StatusCode}",
            statusCode);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private (int StatusCode, ErrorResponse Response) MapException(Exception exception)
    {
        return exception switch
        {
            PasteNotFoundException ex => (StatusCodes.Status404NotFound, new ErrorResponse
            {
                Error = ex.Message
            }),

            PasteExpiredException ex => (StatusCodes.Status410Gone, new ErrorResponse
            {
                Error = "Paste has expired",
                Details = new
                {
                    hash = ex.Hash,
                    expiredAt = ex.ExpiredAt
                }
            }),

            InvalidPasteException ex => (StatusCodes.Status400BadRequest, new ErrorResponse
            {
                Error = ex.Message
            }),

            DomainException ex => (StatusCodes.Status400BadRequest, new ErrorResponse
            {
                Error = ex.Message
            }),

            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Internal server error",
                Details = _environment.IsDevelopment() ? exception.Message : null
            })
        };
    }
}

