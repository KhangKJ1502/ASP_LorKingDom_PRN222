using BLL.Interfaces;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ASP_LorKingDom.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly ISystemHealthService _health;

        public HealthController(ISystemHealthService health)
        {
            _health = health;
        }

        // GET /health/db
        [HttpGet("db")]
        public async Task<IActionResult> Db(CancellationToken ct)
        {
            var report = await _health.CheckDatabaseAsync(ct);
            if (report.Ok) return Ok(report);
            return StatusCode(500, report);
        }
    }
}
