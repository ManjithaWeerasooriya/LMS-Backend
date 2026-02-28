using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LMS_Backend.Data;
using LMS_Backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace LMS_Backend.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDBContext _db;

    public TokenService(IConfiguration config, UserManager<User> userManager, ApplicationDBContext db)
    {
        _config = config;
        _userManager = userManager;
        _db = db;
    }

    public async Task<(string accessToken, int expiresInSeconds)> CreateAccessTokenAsync(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Student";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Role, primaryRole),
            new("status", user.Status.ToString()),

            // used to invalidate old JWTs after password change
            new("AspNet.Identity.SecurityStamp", user.SecurityStamp ?? string.Empty)
        };

        var minutes = int.Parse(jwt["AccessTokenMinutes"]!);
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, minutes * 60);
    }

    public async Task<string> CreateAndStoreRefreshTokenAsync(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var days = int.Parse(jwt["RefreshTokenDays"]!);

        // random 64 bytes -> base64
        var bytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(bytes);

        var entity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<User?> ValidateRefreshTokenAsync(string refreshToken, string deviceId)
    {
        var rt = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken && x.DeviceId == deviceId);

        if (rt == null) return null;
        if (rt.RevokedAt != null) return null;
        if (rt.ExpiresAt < DateTime.UtcNow) return null;

        rt.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return rt.User;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string deviceId)
    {
        var rt = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken && x.DeviceId == deviceId);
        
        if (rt == null) return;

        rt.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensForUserAsync(string userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync();

        if (tokens.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> CreateOrReplaceRefreshTokenAsync(User user, string deviceId, string? userAgent, string? ip)
    {
        var jwt = _config.GetSection("Jwt");
        var days = int.Parse(jwt["RefreshTokenDays"]!);

        var bytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(bytes);

        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId);

        if (existing == null)
        {
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                DeviceId = deviceId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                UserAgent = userAgent,
                IpAddress = ip,
                LastUsedAt = DateTime.UtcNow
            });
        }
        else
        {
            // Replace token in the same row (keeps table size stable)
            existing.Token = refreshToken;
            existing.ExpiresAt = DateTime.UtcNow.AddDays(days);
            existing.RevokedAt = null;
            existing.UserAgent = userAgent;
            existing.IpAddress = ip;
            existing.LastUsedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return refreshToken;
    }
}
