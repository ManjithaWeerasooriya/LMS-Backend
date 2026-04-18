using LMS_Backend.Infrastructure.Auth;
using LMS_Backend.Models.DTOs.Admin;
using LMS_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS_Backend.Controllers;

[ApiController]
[Route("api/v1/admin/system")]
[Authorize(Policy = AppPolicies.TeacherOnly)]
public class AdminSystemController : ControllerBase
{
    private readonly AdminDiagnosticsService _adminDiagnosticsService;

    public AdminSystemController(AdminDiagnosticsService adminDiagnosticsService)
    {
        _adminDiagnosticsService = adminDiagnosticsService;
    }

    [HttpPost("test-azure-connections")]
    public async Task<ActionResult<AzureConnectionDiagnosticsResponseDto>> TestAzureConnections(
        CancellationToken cancellationToken)
    {
        var result = await _adminDiagnosticsService.TestAzureConnectionsAsync(cancellationToken);
        return Ok(result);
    }
}
