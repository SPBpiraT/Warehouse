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
            var balances = _memoryCache.Get<List<Balance>>("BalE") ?? new List<Balance>();
            var resources = _memoryCache.Get<List<Resource>>("ResE") ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("UnitE") ?? new List<Unit>();


            if (resource != null && resource != 0)
            {
                balances = (List<Balance>)balances.Where(b => b.ResourceId == resource);
            }
            if (unit != null && unit != 0)
            {
                balances = (List<Balance>)balances.Where(b => b.UnitId == unit);
            }

            var vm = new BalancesListViewModel()
            {
                BalancesList = balances,
                Resources = new SelectList(resources, "Id", "Title", resource),
                Units = new SelectList(units, "Id", "Title", unit),
            };

            return View(vm);

        }

        public async Task<IActionResult> MakeTestData()
        {
            var res = new List<Resource>
            {
                new Resource
                {
                    Id = 1,
                    Title = "Test1",
                    IsActive = true
                },
                new Resource
                {
                    Id = 2,
                    Title = "Test2",
                    IsActive = true
                },
                new Resource
                {
                    Id = 3,
                    Title = "Test3",
                    IsActive = true
                },
                new Resource
                {
                    Id = 4,
                    Title = "Test4",
                    IsActive = false
                },
                new Resource
                {
                    Id = 5,
                    Title = "Test5",
                    IsActive = false
                },
            };

            var units = new List<Unit>
            {
                new Unit
                {
                    Id = 1,
                    Title = "unit1",
                    IsActive = true
                },
                new Unit
                {
                    Id = 2,
                    Title = "unit2",
                    IsActive = true
                },
                new Unit
                {
                    Id = 3,
                    Title = "unit3",
                    IsActive = true
                },
                new Unit
                {
                    Id = 4,
                    Title = "unit4",
                    IsActive = false
                },
                new Unit
                {
                    Id = 5,
                    Title = "unit5",
                    IsActive = false
                }
            };

            var bal = new List<Balance>
            {
                new Balance
                {
                    Id = 1,
                    ResourceId = 1,
                    UnitId = 1,
                    Quantity = 100
                },
                new Balance
                {
                    Id = 2,
                    ResourceId = 2,
                    UnitId = 2,
                    Quantity = 100
                },
                new Balance
                {
                    Id = 3,
                    ResourceId = 3,
                    UnitId = 3,
                    Quantity = 111
                }
            };

            _memoryCache.Set("ResE", res);
            _memoryCache.Set("UnitE", units);
            _memoryCache.Set("BalE", bal);

            return RedirectToAction(nameof(Index));
        }

    }
}
