using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Receipt;

namespace Warehouse.Web.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly ILogger<ReceiptController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ReceiptController(ILogger<ReceiptController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index([Bind("DateRange,SelectedNumbers,SelectedResources,SelectedUnits")] ReceiptsListViewModel receiptsListView)
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();

            if (receiptsListView.DateRange is not null)
            {
                var dates = receiptsListView.DateRange.Split(" - ");
                if (DateTime.TryParse(dates[0], out DateTime start) &&
                    DateTime.TryParse(dates[1], out DateTime end))
                {
                    Console.WriteLine(start);
                }
            }

            var vm = new ReceiptsListViewModel()
            {
                Receipts = receipts,
                ReceiptNumbers = receipts.Select(r => new SelectListItem
                {
                    Text = r.Number.ToString(),
                    Value = r.Id.ToString()
                }),
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
                SelectedNumbers = receiptsListView.SelectedNumbers ?? new List<int>(),
                SelectedResources = receiptsListView.SelectedResources ?? new List<int>(),
                SelectedUnits = receiptsListView.SelectedUnits ?? new List<int>()
            };
            return View(vm);
        }

        public async Task<IActionResult> MakeTestData()
        {
            var resources = new List<Resource>()
            {
                new Resource()
                {
                    Id = 1,
                    Title = "Banana",
                    IsActive = true
                },
                new Resource()
                {
                    Id = 2,
                    Title = "Apple",
                    IsActive = true
                }
            };

            var units = new List<Unit>()
            {
                new Unit()
                {
                    Id = 1,
                    Title = "kg",
                    IsActive = true
                },
                new Unit()
                {
                    Id = 2,
                    Title = "box",
                    IsActive = true
                }
            };

            var receipts = new List<Receipt>()
            {
                new Receipt
                {
                    Id = 1,
                    Number = 1,
                    Date = DateOnly.MinValue,
                    ReceiptItems = new List<ReceiptItem>()
                    {
                        new ReceiptItem()
                        {
                            Id = 1,
                            ReceiptId = 1,
                            ResourceId = 1,
                            UnitId = 1,
                            Quantity = 1,
                            Resource = resources[0],
                            Unit = units[0]
                        },
                        new ReceiptItem()
                        {
                            Id = 2,
                            ReceiptId = 1,
                            ResourceId = 2,
                            UnitId = 2,
                            Quantity = 2,
                            Resource = resources[1],
                            Unit = units[1]
                        }
                    }
                },
                new Receipt
                {
                    Id = 2,
                    Number = 2,
                    Date = DateOnly.MaxValue,
                    ReceiptItems = new List<ReceiptItem>()
                    {
                        new ReceiptItem()
                        {
                            Id = 3,
                            ReceiptId = 2,
                            ResourceId = 1,
                            UnitId = 2,
                            Quantity = 3,
                            Resource = resources[0],
                            Unit = units[1]

                        }
                    }
                }
            };
          
            _memoryCache.Set("Receipts", receipts);
            _memoryCache.Set("Resources", resources);
            _memoryCache.Set("Units", units);

            return RedirectToAction(nameof(Index));
        }
    }
}
