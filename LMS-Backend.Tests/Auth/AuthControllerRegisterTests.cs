using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LMS_Backend.Tests.Auth;

public class AuthControllerRegisterTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly TokenService _tokenService;
    private readonly ApplicationDBContext _dbContext;

    public AuthControllerRegisterTests()
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

        _configurationMock = new Mock<IConfiguration>();
        _emailSenderMock = new Mock<IEmailSender>();

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("token");

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock
            .Setup(r => r.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _roleManagerMock
            .Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);

        _emailSenderMock
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var inMemoryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "UnitTestKey1234567890!",
                ["Jwt:Issuer"] = "UnitTestIssuer",
                ["Jwt:Audience"] = "UnitTestAudience",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDBContext(dbOptions);
        _tokenService = new TokenService(inMemoryConfig, _userManagerMock.Object, _dbContext);
    }

    private AuthController CreateController()
    {
        var controller = new AuthController(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _signInManagerMock.Object,
            _tokenService,
            _configurationMock.Object,
            _emailSenderMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://example.com/confirm");

        controller.Url = urlHelperMock.Object;

        return controller;
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRoleIsMissing()
    {
        var controller = CreateController();
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = " "
        };

        var result = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRoleIsInvalid()
    {
        var controller = CreateController();
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = "Admin"
        };

        var result = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var controller = CreateController();
        var existingUser = new User { Email = "user@example.com", UserName = "user@example.com" };

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(existingUser.Email!))
            .ReturnsAsync(existingUser);

        var request = new RegisterRequest
        {
            Email = existingUser.Email!,
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = "Student"
        };

        var result = await controller.Register(request);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenIdentityCreateFails()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("user@example.com"))
            .ReturnsAsync((User?)null);

        var identityResult = IdentityResult.Failed(new IdentityError { Description = "error" });
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = "Student"
        };

        var result = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_CreatesActiveStudent_WhenRoleIsStudent()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("student@example.com"))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("token");

        _roleManagerMock
            .Setup(r => r.RoleExistsAsync("Student"))
            .ReturnsAsync(true);

        var request = new RegisterRequest
        {
            Email = "student@example.com",
            Password = "Password123!",
            FirstName = "Student",
            LastName = "User",
            Role = "Student"
        };

        var result = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!;
        var role = (string)body.GetType().GetProperty("role")!.GetValue(body)!;
        Assert.Equal("Student", role);
    }

    [Fact]
    public async Task Register_CreatesActiveTeacher_WhenRoleIsTeacher()
    {
        var controller = CreateController();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync("teacher@example.com"))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("token");

        _roleManagerMock
            .Setup(r => r.RoleExistsAsync("Teacher"))
            .ReturnsAsync(true);

        var request = new RegisterRequest
        {
            Email = "teacher@example.com",
            Password = "Password123!",
            FirstName = "Teacher",
            LastName = "User",
            Role = "Teacher"
        };

        var result = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!;
        var message = (string)body.GetType().GetProperty("message")!.GetValue(body)!;
        var role = (string)body.GetType().GetProperty("role")!.GetValue(body)!;
        var status = (string)body.GetType().GetProperty("status")!.GetValue(body)!;

        Assert.Equal("Registered successfully.", message);
        Assert.Equal("Teacher", role);
        Assert.Equal("Active", status);
    }
}
