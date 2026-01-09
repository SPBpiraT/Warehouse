using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Warehouse.Infrastructure;
using Warehouse.Web.ViewModels.Balance;

namespace Warehouse.Web.Controllers
{
    public class BalanceController : Controller
    {
        private readonly ILogger<BalanceController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        public BalanceController(ILogger<BalanceController> logger,
            WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
        }
        public async Task<IActionResult> Index([Bind("SelectedResources,SelectedUnits")] BalancesListViewModel balancesListView)
        {
            //UoW GetAll
            //TODO: Cache results
            var balances = await _warehouseDbContext.Balances.ToListAsync();
            var resources = await _warehouseDbContext.Resources.ToListAsync();
            var units = await _warehouseDbContext.Units.ToListAsync();

            //Filter
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
                balance.Resource = resources.FirstOrDefault(r => r.Id == balance.ResourceId);
                balance.Unit = units.FirstOrDefault(u => u.Id == balance.UnitId);
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
