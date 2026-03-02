using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMS_Backend.Controllers;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Auth;
using LMS_Backend.Models.Entities;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LMS_Backend.Tests.Auth;

public class AuthControllerLoginTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly TokenService _tokenService;
    private readonly ApplicationDBContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly Mock<IEmailSender> _emailSenderMock;

    public AuthControllerLoginTests()
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
            _emailSenderMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("notfound@example.com"))
            .ReturnsAsync((User?)null);

        var req = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "Password123!",
            DeviceId = "device1"
        };

        var result = await controller.Login(req);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = unauthorized.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotActive()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            Status = UserStatus.Pending,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        var req = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!",
            DeviceId = "device1"
        };

        var result = await controller.Login(req);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = unauthorized.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.StartsWith("User is", message);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailNotConfirmed()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            Status = UserStatus.Active,
            EmailConfirmed = false
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        var req = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!",
            DeviceId = "device1"
        };

        var result = await controller.Login(req);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = unauthorized.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("Please verify your email before logging in.", message);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        var controller = CreateController();

        var user = new User
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var req = new LoginRequest
        {
            Email = user.Email!,
            Password = "WrongPassword!",
            DeviceId = "device1"
        };

        var result = await controller.Login(req);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var body = unauthorized.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithTokens_WhenSuccessful()
    {
        var controller = CreateController();

        var user = new User
        {
            Id = "user-id",
            Email = "user@example.com",
            UserName = "user@example.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Student" });

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var req = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!",
            DeviceId = "device1"
        };

        var result = await controller.Login(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!;
        var accessToken = (string)body.GetType().GetProperty("accessToken")!.GetValue(body)!;
        var refreshToken = (string)body.GetType().GetProperty("refreshToken")!.GetValue(body)!;
        var tokenType = (string)body.GetType().GetProperty("tokenType")!.GetValue(body)!;

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.Equal("Bearer", tokenType);
    }
}
