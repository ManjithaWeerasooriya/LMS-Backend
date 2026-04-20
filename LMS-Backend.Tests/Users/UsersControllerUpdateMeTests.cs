using System.Security.Claims;
using System.Text;
using LMS_Backend.Controllers;
using LMS_Backend.Models.DTOs.User;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace LMS_Backend.Tests.Users;

public class UsersControllerUpdateMeTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;

    public UsersControllerUpdateMeTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);

        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<UsersController>>();
    }

    private UsersController CreateControllerWithUser(User? user)
    {
        var controller = new UsersController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            null!,
            _emailSenderMock.Object,
            _loggerMock.Object);

        var httpContext = new DefaultHttpContext();

        if (user != null)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            }, "TestAuth");

            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task UpdateMe_ReturnsUnauthorized_WhenUserIdMissing()
    {
        var controller = CreateControllerWithUser(null);

        var request = new UpdateMyProfileRequest
        {
            FirstName = "New",
            LastName = "Name",
            Phone = "123"
        };

        var result = await controller.UpdateMe(request, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMe_ReturnsUnauthorized_WhenUserNotFound()
    {
        var user = new User { Id = "user-id" };
        var controller = CreateControllerWithUser(user);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync((User?)null);

        var request = new UpdateMyProfileRequest
        {
            FirstName = "New",
            LastName = "Name",
            Phone = "123"
        };

        var result = await controller.UpdateMe(request, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMe_ReturnsBadRequest_WhenUpdateFails()
    {
        var user = new User { Id = "user-id" };

        var controller = CreateControllerWithUser(user);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "error" }));

        var result = await controller.UpdateMe(new UpdateMyProfileRequest(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = badRequest.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;

        Assert.Equal("Profile update failed.", message);
    }

    [Fact]
    public async Task UpdateMe_UpdatesAllowedFields_AndReturnsProfile()
    {
        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            Status = UserStatus.Active
        };

        var controller = CreateControllerWithUser(user);

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await controller.UpdateMe(new UpdateMyProfileRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<UserProfileRequest>(ok.Value);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsValidationProblem_WhenModelStateInvalid()
    {
        var controller = CreateControllerWithUser(new User { Id = "user-id" });
        var serviceMock = new Mock<IProfileImageService>();

        controller.ModelState.AddModelError("File", "Required");

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest(),
            serviceMock.Object,
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsUnauthorized_WhenCurrentUserMissing()
    {
        var controller = CreateControllerWithUser(null);
        var serviceMock = new Mock<IProfileImageService>();

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("img.png", "image/png") },
            serviceMock.Object,
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsUpdatedProfile_WhenSuccessful()
    {
        var user = new User { Id = "user-id", Status = UserStatus.Active };

        var controller = CreateControllerWithUser(user);
        var serviceMock = new Mock<IProfileImageService>();

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        serviceMock
            .Setup(s => s.UploadProfileImageAsync(user.Id, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = user.Id, ProfileImageUrl = "url" });

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("img.png", "image/png") },
            serviceMock.Object,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileRequest>(ok.Value);

        Assert.Equal("url", dto.ProfileImageUrl);
    }

    // 🔴 NEW TESTS

    [Fact]
    public async Task UploadProfileImage_ReturnsBadRequest_WhenInvalidFileType()
    {
        var user = new User { Id = "user-id" };
        var controller = CreateControllerWithUser(user);
        var serviceMock = new Mock<IProfileImageService>();

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        serviceMock
            .Setup(s => s.UploadProfileImageAsync(user.Id, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Only JPG and PNG images are allowed."));

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("file.txt", "text/plain") },
            serviceMock.Object,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsBadRequest_WhenFileTooLarge()
    {
        var user = new User { Id = "user-id" };
        var controller = CreateControllerWithUser(user);
        var serviceMock = new Mock<IProfileImageService>();

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        serviceMock
            .Setup(s => s.UploadProfileImageAsync(user.Id, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("File too large."));

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateLargeFormFile() },
            serviceMock.Object,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsInternalServerError_WhenServiceFails()
    {
        var user = new User { Id = "user-id" };
        var controller = CreateControllerWithUser(user);
        var serviceMock = new Mock<IProfileImageService>();

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        serviceMock
            .Setup(s => s.UploadProfileImageAsync(user.Id, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("img.png", "image/png") },
            serviceMock.Object,
            CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes("data");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static IFormFile CreateLargeFormFile()
    {
        var bytes = new byte[6 * 1024 * 1024];
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "large.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
}