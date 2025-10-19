using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
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
            var balancesCache = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

            var balances = JsonSerializer.Deserialize<List<Balance>>(JsonSerializer.Serialize(balancesCache));

            // Фильтрация
            if (balancesListView.SelectedResources != null && balancesListView.SelectedResources.Count() > 0)
            {
                balances = balances.Where(b => balancesListView.SelectedResources.Contains(b.ResourceId)).ToList();
            }

            if (balancesListView.SelectedUnits != null && balancesListView.SelectedUnits.Count() > 0)
            {
                balances = balances.Where(b => balancesListView.SelectedUnits.Contains(b.UnitId)).ToList();
            }

            foreach (var balance in balances)
            {
                balance.Resource = resources.SingleOrDefault(r => r.Id == balance.ResourceId);
                balance.Unit = units.SingleOrDefault(u => u.Id == balance.UnitId);
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
    }
}
