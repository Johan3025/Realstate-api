using Microsoft.AspNetCore.Mvc;
using RealState.Services.Exceptions;
using System.Net;
using System.Text.Json;

namespace RealState.Api.Middleware
{
    /// <summary>
    /// Global exception handler. Catches all unhandled exceptions and returns
    /// a consistent <see cref="ProblemDetails"/> (RFC 7807) response.
    /// No try/catch should exist in controllers.
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
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
            catch (ValidationException ex)
            {
                await WriteProblemAsync(context, (int)HttpStatusCode.BadRequest, "Validation Error", ex.Message);
            }
            catch (NotFoundException ex)
            {
                await WriteProblemAsync(context, (int)HttpStatusCode.NotFound, "Not Found", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, (int)HttpStatusCode.InternalServerError,
                    "Internal Server Error", "An unexpected error occurred. Please try again later.");
            }
        }

        private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
