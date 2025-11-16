using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)]
    [Route("api/admin/logs")]
    public class AdminLogsController : ControllerBase
    {
        private readonly IAdminLogService _logService;

        public AdminLogsController(IAdminLogService logService)
        {
            _logService = logService;
        }

        [HttpGet("audit")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParameters query)
        {
            var result = await _logService.GetAuditLogsAsync(query ?? new AuditLogQueryParameters());
            return Ok(result);
        }

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorLogs([FromQuery] ErrorLogQueryParameters query)
        {
            var result = await _logService.GetErrorLogsAsync(query ?? new ErrorLogQueryParameters());
            return Ok(result);
        }

        [HttpGet("system")]
        public async Task<IActionResult> GetSystemLogs([FromQuery] SystemLogQueryParameters query)
        {
            var result = await _logService.GetSystemLogsAsync(query ?? new SystemLogQueryParameters());
            return Ok(result);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventoryLogs([FromQuery] InventoryLogQueryParameters query)
        {
            var result = await _logService.GetInventoryLogsAsync(query ?? new InventoryLogQueryParameters());
            return Ok(result);
        }
    }
}
