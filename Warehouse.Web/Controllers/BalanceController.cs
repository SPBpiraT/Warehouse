using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Warehouse.Web.Controllers
{
    public class BalanceController : Controller
    {
        private readonly ILogger<BalanceController> _logger;
        private readonly IMemoryCache _memoryCache;

        public BalanceController(ILogger<BalanceController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }
    }
}
