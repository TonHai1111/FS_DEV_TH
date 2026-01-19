using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Middleware;
using TodoApi.Models.DTOs;

namespace TodoApi.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock;

    public ExceptionMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private async Task<ApiResponse?> GetResponseFromContext(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("Access denied");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.False(response?.Success);
        Assert.Equal("Access denied", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_Returns400()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Invalid operation");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.Equal("Invalid operation", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_Returns404()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new KeyNotFoundException("Resource not found");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.Equal("Resource not found", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new ArgumentException("Invalid argument");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.Equal("Invalid argument", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenDbUpdateConcurrencyException_Returns409()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new DbUpdateConcurrencyException("Concurrency conflict");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.Contains("modified by another request", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenFormatException_Returns400()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new FormatException("Bad format");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        Assert.Contains("Invalid data format", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnknownException_Returns500WithGenericMessage()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new Exception("Some internal error");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        var response = await GetResponseFromContext(context);
        // Should NOT expose internal error message
        Assert.Equal("An unexpected error occurred. Please try again later.", response?.Message);
    }

    [Fact]
    public async Task InvokeAsync_SetsJsonContentType()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new Exception("Error");
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_LogsException()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Test error");
        RequestDelegate next = (ctx) => throw exception;
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify logging was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
