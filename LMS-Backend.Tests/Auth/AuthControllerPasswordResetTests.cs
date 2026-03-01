using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LMS_Backend.Controllers;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LMS_Backend.Tests.Auth;

public class AuthControllerPasswordResetTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly TokenService _tokenService;
    private readonly ApplicationDBContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly Mock<IEmailSender> _emailSenderMock;

    public AuthControllerPasswordResetTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            null!, null!, null!, null!);

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

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDBContext(options);
        _tokenService = new TokenService(_configuration, _userManagerMock.Object, _dbContext);
        _emailSenderMock = new Mock<IEmailSender>();
    }

    private AuthController CreateController()
    {
        var controller = new AuthController(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _signInManagerMock.Object,
            _tokenService,
            _configuration,
            _emailSenderMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://example.com/reset");

        controller.Url = urlHelperMock.Object;

        return controller;
    }

    [Fact]
    public async Task ForgotPassword_SendsEmail_WhenAccountExistsAndConfirmed()
    {
        var controller = CreateController();

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@example.com",
            EmailConfirmed = true,
            FirstName = "Test"
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        var request = new ForgotPasswordRequest { Email = user.Email! };

        var result = await controller.ForgotPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("If an account with this email exists, a password reset link has been sent.", message);

        _emailSenderMock.Verify(
            e => e.SendEmailAsync(
                user.Email!,
                It.Is<string>(s => s.Contains("Reset")),
                It.Is<string>(html => html.Contains("Reset my password"))),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_DoesNotSendEmail_WhenAccountMissingOrUnconfirmed()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((User?)null);

        var resultMissing = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "missing@example.com" });
        AssertOkWithGenericMessage(resultMissing);

        var unconfirmedUser = new User
        {
            Id = "user-id",
            Email = "pending@example.com",
            EmailConfirmed = false
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(unconfirmedUser.Email!))
            .ReturnsAsync(unconfirmedUser);

        var resultUnconfirmed = await controller.ForgotPassword(new ForgotPasswordRequest { Email = unconfirmedUser.Email! });
        AssertOkWithGenericMessage(resultUnconfirmed);

        _emailSenderMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        var controller = CreateController();

        var request = new ResetPasswordRequest
        {
            UserId = "user-id",
            Token = "token",
            NewPassword = "Password123!",
            ConfirmPassword = "Mismatch!"
        };

        var result = await controller.ResetPassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var message = (string)badRequest.Value!.GetType().GetProperty("message")!.GetValue(badRequest.Value)!;
        Assert.Equal("NewPassword and ConfirmPassword must match.", message);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenUserNotFound()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByIdAsync("missing"))
            .ReturnsAsync((User?)null);

        var request = new ResetPasswordRequest
        {
            UserId = "missing",
            Token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token")),
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await controller.ResetPassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var message = (string)badRequest.Value!.GetType().GetProperty("message")!.GetValue(badRequest.Value)!;
        Assert.Equal("Invalid password reset token or user.", message);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenTokenIsInvalidBase64()
    {
        var controller = CreateController();

        var user = new User { Id = "user-id" };
        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        var request = new ResetPasswordRequest
        {
            UserId = user.Id,
            Token = "%%%invalid%%%",
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await controller.ResetPassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var message = (string)badRequest.Value!.GetType().GetProperty("message")!.GetValue(badRequest.Value)!;
        Assert.Equal("Invalid password reset token.", message);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenIdentityResetFails()
    {
        var controller = CreateController();

        var user = new User { Id = "user-id" };
        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.ResetPasswordAsync(user, "decoded-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad token" }));

        var request = new ResetPasswordRequest
        {
            UserId = user.Id,
            Token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("decoded-token")),
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await controller.ResetPassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = badRequest.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("Password reset failed.", message);
    }

    [Fact]
    public async Task ResetPassword_Succeeds_AndRevokesRefreshTokens()
    {
        var controller = CreateController();

        var user = new User { Id = Guid.NewGuid().ToString() };
        _userManagerMock
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.ResetPasswordAsync(user, "good-token", "Password123!"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "stale-token",
            DeviceId = "test-device",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await _dbContext.SaveChangesAsync();

        var request = new ResetPasswordRequest
        {
            UserId = user.Id,
            Token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("good-token")),
            NewPassword = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await controller.ResetPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var message = (string)ok.Value!.GetType().GetProperty("message")!.GetValue(ok.Value)!;
        Assert.Equal("Password has been reset successfully. You can now sign in with the new password.", message);

        _userManagerMock.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);

        var storedToken = _dbContext.RefreshTokens.Single();
        Assert.NotNull(storedToken.RevokedAt);
    }

    private static void AssertOkWithGenericMessage(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("If an account with this email exists, a password reset link has been sent.", message);
    }
}
