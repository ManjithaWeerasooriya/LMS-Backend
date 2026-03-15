using LMS_Backend.Models.DTOs.Public;

namespace LMS_Backend.Services
{
    public interface IPublicService
    {
        Task<PlatformStatsDto> GetPlatformStatsAsync();
    }
}