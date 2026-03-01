using System.Security.Claims;
using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Admin;
using LMS_Backend.Models.Entities;
using LMS_Backend.Models.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS_Backend.Services;

public class AdminService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ApplicationDBContext dbContext,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of users filtered by role and status for admin dashboards.
    /// </summary>
    /// <param name="query">Pagination and filtering parameters.</param>
    /// <returns>Paged user information.</returns>
    public async Task<UserListResponseDto> GetUsersAsync(UserQueryParametersDto query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        IQueryable<User> usersQuery = _dbContext.Users;

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<UserStatus>(query.Status, true, out var statusFilter))
        {
            usersQuery = usersQuery.Where(u => u.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = query.Role.Trim().ToUpperInvariant();
            var roleIds = await _dbContext.Roles
                .Where(r => r.NormalizedName == normalizedRole)
                .Select(r => r.Id)
                .ToListAsync();

            if (roleIds.Count == 0)
            {
                return new UserListResponseDto
                {
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Users = new List<UserListItemDto>()
                };
            }

            usersQuery = usersQuery.Where(u => _dbContext.UserRoles
                .Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
        }

        var totalCount = await usersQuery.CountAsync();
        var skip = (pageNumber - 1) * pageSize;

        var pagedUsers = await usersQuery
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Email)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserListItemDto>(pagedUsers.Count);
        foreach (var user in pagedUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;

            userDtos.Add(new UserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = primaryRole,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            });
        }

        return new UserListResponseDto
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Users = userDtos
        };
    }

    public async Task SuspendUserAsync(string userId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        var actingAdminId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(actingAdminId))
        {
            throw new InvalidOperationException("Unable to determine the acting administrator.");
        }

        if (targetUser.Id == actingAdminId)
        {
            throw new InvalidOperationException("Administrators cannot suspend their own accounts.");
        }

        if (await _userManager.IsInRoleAsync(targetUser, "Admin"))
        {
            throw new InvalidOperationException("Administrators cannot suspend other administrators.");
        }

        if (targetUser.Status == UserStatus.Suspended)
        {
            _logger.LogInformation("Admin {AdminId} attempted to suspend user {UserId} who is already suspended.", actingAdminId, targetUser.Id);
            return;
        }

        targetUser.Status = UserStatus.Suspended;
        var result = await _userManager.UpdateAsync(targetUser);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to suspend user '{targetUser.Email}': {errors}");
        }

        _logger.LogInformation("Admin {AdminId} suspended user {UserId}. Reason: {Reason}", actingAdminId, targetUser.Id, reason ?? "n/a");
    }

    public async Task ReactivateUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        if (targetUser.Status == UserStatus.Active)
        {
            return;
        }

        targetUser.Status = UserStatus.Active;
        var result = await _userManager.UpdateAsync(targetUser);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reactivate user '{targetUser.Email}': {errors}");
        }

        var actingAdminId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(actingAdminId))
        {
            _logger.LogInformation("Admin {AdminId} reactivated user {UserId}.", actingAdminId, targetUser.Id);
        }
    }

    public async Task ApproveTeacherAsync(string userId)
    {
        var targetUser = await GetPendingTeacherAsync(userId);
        targetUser.Status = UserStatus.Active;
        await PersistStatusChangeAsync(targetUser, $"approve teacher '{targetUser.Email}'");

        var actingAdminId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(actingAdminId))
        {
            _logger.LogInformation("Admin {AdminId} approved teacher {TeacherId}.", actingAdminId, targetUser.Id);
        }
    }

    public async Task RejectTeacherAsync(string userId)
    {
        var targetUser = await GetPendingTeacherAsync(userId);
        targetUser.Status = UserStatus.Suspended;
        await PersistStatusChangeAsync(targetUser, $"reject teacher '{targetUser.Email}'");

        var actingAdminId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(actingAdminId))
        {
            _logger.LogInformation("Admin {AdminId} rejected teacher {TeacherId}.", actingAdminId, targetUser.Id);
        }
    }

    private async Task<User> GetPendingTeacherAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }

        if (!await _userManager.IsInRoleAsync(targetUser, "Teacher"))
        {
            throw new InvalidOperationException("Only teacher accounts can be approved or rejected.");
        }

        if (targetUser.Status != UserStatus.Pending)
        {
            throw new InvalidOperationException("Teacher must be in pending status to approve or reject.");
        }

        return targetUser;
    }

    private async Task PersistStatusChangeAsync(User user, string actionDescription)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to {actionDescription}: {errors}");
        }
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
