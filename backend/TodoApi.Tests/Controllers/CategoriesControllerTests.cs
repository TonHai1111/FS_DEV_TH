using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.Models.DTOs;
using TodoApi.Services;

namespace TodoApi.Tests.Controllers;

public class CategoriesControllerTests
{
    private const int TestUserId = 1;

    private Mock<ICategoryService> CreateMockCategoryService()
    {
        return new Mock<ICategoryService>();
    }

    private CategoriesController CreateControllerWithUser(Mock<ICategoryService> mockCategoryService)
    {
        var controller = new CategoriesController(mockCategoryService.Object);

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
    public async Task GetCategories_ReturnsOkWithCategories()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        var categories = new List<CategoryResponse>
        {
            new CategoryResponse { Id = 1, Name = "Work", Color = "#FF0000", TaskCount = 5 },
            new CategoryResponse { Id = 2, Name = "Personal", Color = "#00FF00", TaskCount = 3 }
        };

        mockCategoryService
            .Setup(x => x.GetCategoriesAsync(TestUserId))
            .ReturnsAsync(categories);

        var controller = CreateControllerWithUser(mockCategoryService);

        // Act
        var result = await controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<List<CategoryResponse>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data!.Count);
    }

    [Fact]
    public async Task GetCategory_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        var category = new CategoryResponse { Id = 1, Name = "Work", Color = "#FF0000", TaskCount = 5 };

        mockCategoryService
            .Setup(x => x.GetCategoryAsync(TestUserId, 1))
            .ReturnsAsync(category);

        var controller = CreateControllerWithUser(mockCategoryService);

        // Act
        var result = await controller.GetCategory(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<CategoryResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Work", response.Data!.Name);
    }

    [Fact]
    public async Task GetCategory_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.GetCategoryAsync(TestUserId, 999))
            .ReturnsAsync((CategoryResponse?)null);

        var controller = CreateControllerWithUser(mockCategoryService);

        // Act
        var result = await controller.GetCategory(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Category not found", response.Message);
    }

    [Fact]
    public async Task CreateCategory_WithValidRequest_Returns201Created()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        var category = new CategoryResponse { Id = 1, Name = "New Category", Color = "#FF0000", TaskCount = 0 };

        mockCategoryService
            .Setup(x => x.CategoryNameExistsAsync(TestUserId, "New Category", null))
            .ReturnsAsync(false);
        mockCategoryService
            .Setup(x => x.CreateCategoryAsync(TestUserId, It.IsAny<CreateCategoryRequest>()))
            .ReturnsAsync(category);

        var controller = CreateControllerWithUser(mockCategoryService);
        var request = new CreateCategoryRequest { Name = "New Category", Color = "#FF0000" };

        // Act
        var result = await controller.CreateCategory(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<CategoryResponse>>(objectResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Category created successfully", response.Message);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.CategoryNameExistsAsync(TestUserId, "Existing", null))
            .ReturnsAsync(true);

        var controller = CreateControllerWithUser(mockCategoryService);
        var request = new CreateCategoryRequest { Name = "Existing", Color = "#FF0000" };

        // Act
        var result = await controller.CreateCategory(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("A category with this name already exists", response.Message);
    }

    [Fact]
    public async Task UpdateCategory_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        var category = new CategoryResponse { Id = 1, Name = "Updated Name", Color = "#00FF00", TaskCount = 5 };

        mockCategoryService
            .Setup(x => x.CategoryNameExistsAsync(TestUserId, "Updated Name", 1))
            .ReturnsAsync(false);
        mockCategoryService
            .Setup(x => x.UpdateCategoryAsync(TestUserId, 1, It.IsAny<UpdateCategoryRequest>()))
            .ReturnsAsync(category);

        var controller = CreateControllerWithUser(mockCategoryService);
        var request = new UpdateCategoryRequest { Name = "Updated Name", Color = "#00FF00" };

        // Act
        var result = await controller.UpdateCategory(1, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<CategoryResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Category updated successfully", response.Message);
    }

    [Fact]
    public async Task UpdateCategory_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.CategoryNameExistsAsync(TestUserId, "Updated Name", 999))
            .ReturnsAsync(false);
        mockCategoryService
            .Setup(x => x.UpdateCategoryAsync(TestUserId, 999, It.IsAny<UpdateCategoryRequest>()))
            .ReturnsAsync((CategoryResponse?)null);

        var controller = CreateControllerWithUser(mockCategoryService);
        var request = new UpdateCategoryRequest { Name = "Updated Name", Color = "#00FF00" };

        // Act
        var result = await controller.UpdateCategory(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Category not found", response.Message);
    }

    [Fact]
    public async Task UpdateCategory_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.CategoryNameExistsAsync(TestUserId, "Existing", 1))
            .ReturnsAsync(true);

        var controller = CreateControllerWithUser(mockCategoryService);
        var request = new UpdateCategoryRequest { Name = "Existing", Color = "#00FF00" };

        // Act
        var result = await controller.UpdateCategory(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("A category with this name already exists", response.Message);
    }

    [Fact]
    public async Task DeleteCategory_WhenExists_ReturnsOk()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.DeleteCategoryAsync(TestUserId, 1))
            .ReturnsAsync(true);

        var controller = CreateControllerWithUser(mockCategoryService);

        // Act
        var result = await controller.DeleteCategory(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Category deleted successfully", response.Message);
    }

    [Fact]
    public async Task DeleteCategory_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var mockCategoryService = CreateMockCategoryService();
        mockCategoryService
            .Setup(x => x.DeleteCategoryAsync(TestUserId, 999))
            .ReturnsAsync(false);

        var controller = CreateControllerWithUser(mockCategoryService);

        // Act
        var result = await controller.DeleteCategory(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Category not found", response.Message);
    }
}