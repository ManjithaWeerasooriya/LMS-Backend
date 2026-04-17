using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        var user = new User
        {
            Id = "user-id",
            FirstName = "Old",
            LastName = "Name",
            Phone = "111"
        };

        var controller = CreateControllerWithUser(user);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "error" }));

        var request = new UpdateMyProfileRequest
        {
            FirstName = " New ",
            LastName = " Name ",
            Phone = " 123 "
        };

        var result = await controller.UpdateMe(request, CancellationToken.None);

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
            FirstName = "Old",
            LastName = "Name",
            Phone = "111",
            Status = UserStatus.Active
        };

        var controller = CreateControllerWithUser(user);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        var request = new UpdateMyProfileRequest
        {
            FirstName = " New ",
            LastName = " Name ",
            Phone = " 123 "
        };

        var result = await controller.UpdateMe(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileRequest>(ok.Value);
        Assert.Equal("New", dto.FirstName);
        Assert.Equal("Name", dto.LastName);
        Assert.Equal("123", dto.Phone);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsValidationProblem_WhenModelStateInvalid()
    {
        var user = new User { Id = "user-id" };
        var controller = CreateControllerWithUser(user);
        var profileImageServiceMock = new Mock<IProfileImageService>();
        controller.ModelState.AddModelError("File", "Required");

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest(),
            profileImageServiceMock.Object,
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsUnauthorized_WhenCurrentUserMissing()
    {
        var controller = CreateControllerWithUser(null);
        var profileImageServiceMock = new Mock<IProfileImageService>();

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("avatar.png", "image/png") },
            profileImageServiceMock.Object,
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UploadProfileImage_ReturnsUpdatedProfile_WhenSuccessful()
    {
        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            FirstName = "Old",
            LastName = "Name",
            Phone = "111",
            Status = UserStatus.Active
        };

        var controller = CreateControllerWithUser(user);
        var profileImageServiceMock = new Mock<IProfileImageService>();

        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        profileImageServiceMock
            .Setup(m => m.UploadProfileImageAsync(user.Id, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Status = user.Status,
                ProfileImageUrl = "https://cdn.example.com/profile.png"
            });

        var result = await controller.UploadProfileImage(
            new UploadProfileImageRequest { File = CreateFormFile("avatar.png", "image/png") },
            profileImageServiceMock.Object,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UserProfileRequest>(ok.Value);
        Assert.Equal("https://cdn.example.com/profile.png", dto.ProfileImageUrl);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes("test-image");
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
