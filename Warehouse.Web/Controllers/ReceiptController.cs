using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
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
            var receiptsCache = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
            var receipts = new List<Receipt>(receiptsCache);

            if (receiptsListView.DateRange is not null)
            {
                var dates = receiptsListView.DateRange.Split(" - ");

                if (DateTime.TryParseExact(dates[0], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) &&
                    DateTime.TryParseExact(dates[1], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
                {
                    receipts = receipts.Where(r => r.Date >= start && r.Date <= end).ToList();
                }
                else
                {
                    //TODO: Add error handle
                }
            }

            if (receiptsListView.SelectedNumbers != null && receiptsListView.SelectedNumbers.Count() > 0)
            {
                receipts = receipts.Where(r => receiptsListView.SelectedNumbers.Contains(r.Id)).ToList();
            }

            if (receiptsListView.SelectedResources != null && receiptsListView.SelectedResources.Count() > 0)
            {
                receipts = receipts.Where(r => r.ReceiptItems.Any(i => receiptsListView.SelectedResources.Contains(i.ResourceId))).ToList();             
            }

            if (receiptsListView.SelectedUnits != null && receiptsListView.SelectedUnits.Count() > 0)
            {
                receipts = receipts.Where(r => r.ReceiptItems.Any(i => receiptsListView.SelectedUnits.Contains(i.UnitId))).ToList();
            }

            var vm = new ReceiptsListViewModel()
            {
                Receipts = receipts,
                ReceiptNumbers = receiptsCache.Select(r => new SelectListItem
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
                DateRange = receiptsListView.DateRange ?? "01-01-2024 - 01-01-2025",
                SelectedNumbers = receiptsListView.SelectedNumbers ?? new List<int>(),
                SelectedResources = receiptsListView.SelectedResources ?? new List<int>(),
                SelectedUnits = receiptsListView.SelectedUnits ?? new List<int>()
            };
            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

            var vm = new CreateReceiptViewModel()
            {
                Receipt = new Receipt()
                {
                    Number = receipts.Count() + 1,
                    Date = DateTime.Now, //TODO: Datetime provider
                    ReceiptItems = new List<ReceiptItem>()
                },
                Resources = resources.Select(r => new SelectListItem
                {
                    Text = r.Title,
                    Value = r.Id.ToString()
                }),
                Units = units.Select(u => new SelectListItem
                {
                    Text = u.Title,
                    Value = u.Id.ToString()
                })
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Receipt")] CreateReceiptViewModel receiptModel)
        {
            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

            try
            {
                if (ModelState.IsValid)
                {
                    //TODO: add check for existing entity id if(!_repo.Exist(id))

                    if (receiptModel.Receipt.ReceiptItems != null)
                    {
                        foreach (var receiptItem in receiptModel.Receipt.ReceiptItems)
                        {
                            receiptItem.Resource = resources.Where(r => r.Id == receiptItem.ResourceId).First();
                            receiptItem.Unit = units.Where(u => u.Id == receiptItem.UnitId).First();
                        }
                    }
                    else
                    {
                        receiptModel.Receipt.ReceiptItems = new List<ReceiptItem>();
                    }

                    receipts.Add(receiptModel.Receipt);

                    _memoryCache.Set("Receipts", receipts);
                    TempData["Success"] = "Новое поступление успешно добавлено.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка!");
            }

            receiptModel.Resources = resources.Select(r => new SelectListItem
            {
                Text = r.Title,
                Value = r.Id.ToString()
            });

            receiptModel.Units = units.Select(r => new SelectListItem
            {
                Text = r.Title,
                Value = r.Id.ToString()
            });

            return View(receiptModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
            var receipt = receipts.FirstOrDefault(x => x.Id == id);

            if (receipt == null)
            {
                return NotFound();
            }
            //return View(receipt);
            return RedirectToAction(nameof(Create));
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
                    Date = new(2025, 5, 20),
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
                    Date = new(2025, 5, 27),
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
                },
                new Receipt
                {
                    Id = 3,
                    Number = 3,
                    Date = new(2025, 5, 23),
                    ReceiptItems = new List<ReceiptItem>()
                    {
                        new ReceiptItem()
                        {
                            Id = 4,
                            ReceiptId = 3,
                            ResourceId = 1,
                            UnitId = 2,
                            Quantity = 11,
                            Resource = resources[0],
                            Unit = units[1]
                        },
                        new ReceiptItem()
                        {
                            Id = 5,
                            ReceiptId = 3,
                            ResourceId = 2,
                            UnitId = 2,
                            Quantity = 21,
                            Resource = resources[1],
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
