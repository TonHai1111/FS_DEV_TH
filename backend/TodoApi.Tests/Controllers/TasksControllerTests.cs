using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.Models;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Controllers;

public class TasksControllerTests
{
    private const int TestUserId = 1;

    private Mock<ITaskService> CreateMockTaskService()
    {
        return new Mock<ITaskService>();
    }

    private TasksController CreateControllerWithUser(Mock<ITaskService> mockTaskService)
    {
        var controller = new TasksController(mockTaskService.Object);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    [Fact]
    public async Task GetTasks_ReturnsOkWithTasks()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var tasks = new List<TaskResponse>
        {
            new TaskResponse { Id = 1, Title = "Task 1", Status = Models.TaskStatus.Todo },
            new TaskResponse { Id = 2, Title = "Task 2", Status = Models.TaskStatus.InProgress }
        };

        mockTaskService
            .Setup(x => x.GetTasksAsync(TestUserId, It.IsAny<TaskFilterParams>()))
            .ReturnsAsync(tasks);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.GetTasks(new TaskFilterParams());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<List<TaskResponse>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Count);
    }

    [Fact]
    public async Task GetTasks_PassesFiltersToService()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.GetTasksAsync(TestUserId, It.IsAny<TaskFilterParams>()))
            .ReturnsAsync(new List<TaskResponse>());

        var controller = CreateControllerWithUser(mockTaskService);
        var filters = new TaskFilterParams
        {
            Status = Models.TaskStatus.Todo,
            Priority = TaskPriority.High,
            Search = "test"
        };

        // Act
        await controller.GetTasks(filters);

        // Assert
        mockTaskService.Verify(x => x.GetTasksAsync(
            TestUserId,
            It.Is<TaskFilterParams>(f =>
                f.Status == Models.TaskStatus.Todo &&
                f.Priority == TaskPriority.High &&
                f.Search == "test"
            )), Times.Once);
    }

    [Fact]
    public async Task GetTask_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var task = new TaskResponse { Id = 1, Title = "Task 1", Status = Models.TaskStatus.Todo };

        mockTaskService
            .Setup(x => x.GetTaskAsync(TestUserId, 1))
            .ReturnsAsync(task);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.GetTask(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TaskResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Task 1", response.Data!.Title);
    }

    [Fact]
    public async Task GetTask_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.GetTaskAsync(TestUserId, 999))
            .ReturnsAsync((TaskResponse?)null);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.GetTask(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Task not found", response.Message);
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_Returns201Created()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var task = new TaskResponse { Id = 1, Title = "New Task", Status = Models.TaskStatus.Todo };

        mockTaskService
            .Setup(x => x.CreateTaskAsync(TestUserId, It.IsAny<CreateTaskRequest>()))
            .ReturnsAsync(task);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new CreateTaskRequest { Title = "New Task" };

        // Act
        var result = await controller.CreateTask(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<TaskResponse>>(objectResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Task created successfully", response.Message);
    }

    [Fact]
    public async Task CreateTask_WithInvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.ValidateCategoryOwnershipAsync(TestUserId, It.IsAny<int>()))
            .ReturnsAsync(false);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new CreateTaskRequest { Title = "New Task", CategoryId = 999 };

        // Act
        var result = await controller.CreateTask(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid category", response.Message);
    }

    [Fact]
    public async Task CreateTask_WithValidCategory_ValidatesOwnership()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var task = new TaskResponse { Id = 1, Title = "New Task", Status = Models.TaskStatus.Todo };

        mockTaskService
            .Setup(x => x.ValidateCategoryOwnershipAsync(TestUserId, 5))
            .ReturnsAsync(true);
        mockTaskService
            .Setup(x => x.CreateTaskAsync(TestUserId, It.IsAny<CreateTaskRequest>()))
            .ReturnsAsync(task);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new CreateTaskRequest { Title = "New Task", CategoryId = 5 };

        // Act
        await controller.CreateTask(request);

        // Assert
        mockTaskService.Verify(x => x.ValidateCategoryOwnershipAsync(TestUserId, 5), Times.Once);
    }

    [Fact]
    public async Task UpdateTask_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var task = new TaskResponse { Id = 1, Title = "Updated Task", Status = Models.TaskStatus.InProgress };

        mockTaskService
            .Setup(x => x.UpdateTaskAsync(TestUserId, 1, It.IsAny<UpdateTaskRequest>()))
            .ReturnsAsync(task);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new UpdateTaskRequest { Title = "Updated Task", Status = Models.TaskStatus.InProgress };

        // Act
        var result = await controller.UpdateTask(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TaskResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Task updated successfully", response.Message);
    }

    [Fact]
    public async Task UpdateTask_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.UpdateTaskAsync(TestUserId, 999, It.IsAny<UpdateTaskRequest>()))
            .ReturnsAsync((TaskResponse?)null);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new UpdateTaskRequest { Title = "Updated Task" };

        // Act
        var result = await controller.UpdateTask(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task UpdateTask_WithInvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.ValidateCategoryOwnershipAsync(TestUserId, It.IsAny<int>()))
            .ReturnsAsync(false);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new UpdateTaskRequest { Title = "Updated Task", CategoryId = 999 };

        // Act
        var result = await controller.UpdateTask(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid category", response.Message);
    }

    [Fact]
    public async Task UpdateTaskStatus_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var task = new TaskResponse { Id = 1, Title = "Task", Status = Models.TaskStatus.Done };

        mockTaskService
            .Setup(x => x.UpdateTaskStatusAsync(TestUserId, 1, Models.TaskStatus.Done))
            .ReturnsAsync(task);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new UpdateTaskStatusRequest { Status = Models.TaskStatus.Done };

        // Act
        var result = await controller.UpdateTaskStatus(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TaskResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(Models.TaskStatus.Done, response.Data!.Status);
    }

    [Fact]
    public async Task UpdateTaskStatus_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.UpdateTaskStatusAsync(TestUserId, 999, It.IsAny<Models.TaskStatus>()))
            .ReturnsAsync((TaskResponse?)null);

        var controller = CreateControllerWithUser(mockTaskService);
        var request = new UpdateTaskStatusRequest { Status = Models.TaskStatus.Done };

        // Act
        var result = await controller.UpdateTaskStatus(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task DeleteTask_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.DeleteTaskAsync(TestUserId, 1))
            .ReturnsAsync(true);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.DeleteTask(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Task deleted successfully", response.Message);
    }

    [Fact]
    public async Task DeleteTask_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        mockTaskService
            .Setup(x => x.DeleteTaskAsync(TestUserId, 999))
            .ReturnsAsync(false);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.DeleteTask(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetTaskStats_ReturnsOkWithStats()
    {
        // Arrange
        var mockTaskService = CreateMockTaskService();
        var stats = new TaskStatsResponse
        {
            Total = 10,
            Todo = 5,
            InProgress = 3,
            Done = 2,
            Overdue = 1
        };

        mockTaskService
            .Setup(x => x.GetTaskStatsAsync(TestUserId))
            .ReturnsAsync(stats);

        var controller = CreateControllerWithUser(mockTaskService);

        // Act
        var result = await controller.GetTaskStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<TaskStatsResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(10, response.Data!.Total);
        Assert.Equal(5, response.Data.Todo);
        Assert.Equal(3, response.Data.InProgress);
        Assert.Equal(2, response.Data.Done);
        Assert.Equal(1, response.Data.Overdue);
    }
}