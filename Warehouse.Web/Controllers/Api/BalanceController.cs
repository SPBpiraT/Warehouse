using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;

namespace Warehouse.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BalanceController : ControllerBase
    {
        private readonly ILogger<BalanceController> _logger;
        private readonly IMemoryCache _memoryCache;

        public BalanceController(ILogger<BalanceController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet("GetBalance")]
        public async Task<IActionResult> GetBalance(int resourceId, int unitId)
        {
            try
            {
                var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();
                var balance = balances.FirstOrDefault(b => b.ResourceId == resourceId && b.UnitId == unitId);

                return Ok(new
                {
                    success = true,
                    balance = balance?.Quantity ?? 0
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
