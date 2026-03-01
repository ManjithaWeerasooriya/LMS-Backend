using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS_Backend.Controllers;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.User;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LMS_Backend.Tests.Users;

public class UsersControllerChangePasswordTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly TokenService _tokenService;
    private readonly ApplicationDBContext _dbContext;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;

    public UsersControllerChangePasswordTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            null!, null!, null!, null!);

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-key-123456789012345678901234567890",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:AccessTokenMinutes"] = "60",
            ["Jwt:RefreshTokenDays"] = "7"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDBContext(options);
        _tokenService = new TokenService(configuration, _userManagerMock.Object, _dbContext);
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<UsersController>>();
    }

    private UsersController CreateController(ClaimsPrincipal? userPrincipal)
    {
        var controller = new UsersController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenService,
            _emailSenderMock.Object,
            _loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        if (userPrincipal != null)
        {
            httpContext.User = userPrincipal;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsValidationProblem_WhenModelStateInvalid()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }, "TestAuth"));

        var controller = CreateController(claims);
        controller.ModelState.AddModelError("NewPassword", "Required");

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = ""
        };

        var result = await controller.ChangeMyPassword(req);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsUnauthorized_WhenUserIdMissing()
    {
        var controller = CreateController(null);

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newPassword123!"
        };

        var result = await controller.ChangeMyPassword(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsUnauthorized_WhenUserNotFound()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }, "TestAuth"));

        var controller = CreateController(claims);

        _userManagerMock
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync((User?)null);

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newPassword123!"
        };

        var result = await controller.ChangeMyPassword(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsForbid_WhenUserNotActive()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }, "TestAuth"));

        var controller = CreateController(claims);

        var user = new User
        {
            Id = "user-id",
            Status = UserStatus.Pending
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newPassword123!"
        };

        var result = await controller.ChangeMyPassword(req);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsBadRequest_WhenChangePasswordFails()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }, "TestAuth"));

        var controller = CreateController(claims);

        var user = new User
        {
            Id = "user-id",
            Status = UserStatus.Active
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "error" }));

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newPassword123!"
        };

        var result = await controller.ChangeMyPassword(req);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = badRequest.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("Password change failed.", message);
    }

    [Fact]
    public async Task ChangeMyPassword_ReturnsNoContent_WhenSuccessful()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-id")
        }, "TestAuth"));

        var controller = CreateController(claims);

        var user = new User
        {
            Id = "user-id",
            Status = UserStatus.Active
        };

        _userManagerMock
            .Setup(m => m.FindByIdAsync("user-id"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var req = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "newPassword123!"
        };

        var result = await controller.ChangeMyPassword(req);

        Assert.IsType<NoContentResult>(result);

        _userManagerMock.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
        _signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);

        // Optional: ensure any existing tokens for this user are revoked
        var tokens = _dbContext.RefreshTokens.Where(t => t.UserId == "user-id").ToList();
        foreach (var token in tokens)
        {
            Assert.NotNull(token.RevokedAt);
        }
    }
}
