using LMS_Backend.Data;
using LMS_Backend.Models.DTOs.Admin;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class AdminService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly UserManager<User> _userManager;

    public AdminService(ApplicationDBContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
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
}
