using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Infrastructure;

namespace Warehouse.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BalanceController : ControllerBase
    {
        private readonly ILogger<BalanceController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        public BalanceController(ILogger<BalanceController> logger, WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
        }

        [HttpGet("GetBalance")]
        public async Task<IActionResult> GetBalance(int resourceId, int unitId)
        {
            try
            {
                var balance = await _warehouseDbContext.Balances
                    .FirstOrDefaultAsync(b => b.ResourceId == resourceId && b.UnitId == unitId);

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
