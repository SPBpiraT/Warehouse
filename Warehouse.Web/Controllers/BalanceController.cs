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
        public async Task<IActionResult> Index([Bind("SelectedResources,SelectedUnits")] BalancesListViewModel balancesListView)
        {

            //UoW GetAll
            var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

            // Фильтрация
            if (balancesListView.SelectedResources != null && balancesListView.SelectedResources.Count() > 0)
            {
                balances = balances.Where(b => balancesListView.SelectedResources.Contains(b.ResourceId)).ToList();
            }

            if (balancesListView.SelectedUnits != null && balancesListView.SelectedUnits.Count() > 0)
            {
                balances = balances.Where(b => balancesListView.SelectedUnits.Contains(b.UnitId)).ToList();
            }

            var vm = new BalancesListViewModel
            {
                Balances = balances,
                Resources = resources.Select(r => new SelectListItem
                {
                    Text = r.Title,
                    Value = r.Id.ToString()
                }),
                Units = units.Select(u => new SelectListItem 
                {
                    Text = u.Title,
                    Value = u.Id.ToString()
                }),
                SelectedResources = balancesListView.SelectedResources ?? new List<int>(),
                SelectedUnits = balancesListView.SelectedUnits ?? new List<int>()
            };

            return View(vm);

        }

        public async Task<IActionResult> MakeTestData()
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive).ToList() ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(r => r.IsActive).ToList() ?? new List<Unit>();
            var bal = new List<Balance>();
            var rand = new Random();
            var unit = new Unit() { Title = "testu" };
            var res = new Resource() { Title = "testr" };
            foreach (var resource in resources)
            {
                var i = 0;
                if(units.Count() > 0) unit = units[rand.Next(units.Count())];
                if(resources.Count() > 0) res = resources[rand.Next(resources.Count())];
                bal.Add(new Balance 
                { 
                    Id = i, 
                    Quantity = (ulong)rand.Next(1000), 
                    ResourceId = res.Id, 
                    Resource = res,
                    UnitId = unit.Id,
                    Unit = unit
                });
                i++;
            }
            
            _memoryCache.Set("Balances", bal);

            return RedirectToAction(nameof(Index));
        }

    }
}
