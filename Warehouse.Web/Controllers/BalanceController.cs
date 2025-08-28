using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Balance;

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
        public async Task<IActionResult> Index(int? resource, int? unit)
        {

            //UoW GetAll
            var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();


            if (resource != null && resource != 0)
            {
                balances = balances.Where(b => b.ResourceId == resource).ToList();
            }
            if (unit != null && unit != 0)
            {
                balances = balances.Where(u => u.UnitId == unit).ToList();
            }

            var vm = new BalancesListViewModel()
            {
                Balances = balances,
                Resources = new SelectList(resources, "Id", "Title", resource),
                Units = new SelectList(units, "Id", "Title", unit),
            };

            return View(vm);

        }

        public async Task<IActionResult> MakeTestData()
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(r => r.IsActive) ?? new List<Unit>();
            var bal = new List<Balance>();
            foreach (var resource in resources)
            {
                bal.Add(new Balance 
                { 
                    Id = resource.Id, 
                    Quantity = 100, 
                    ResourceId = resource.Id, 
                    Resource = resource
                });
            }
            
            _memoryCache.Set("Balances", bal);

            return RedirectToAction(nameof(Index));
        }

    }
}
