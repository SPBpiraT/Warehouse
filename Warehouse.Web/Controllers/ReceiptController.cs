using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Receipt;
using System.Text.Json;

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
            var receiptItemsCache = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();

            var receipts = JsonSerializer.Deserialize<List<Receipt>>(JsonSerializer.Serialize(receiptsCache));
            var receiptItems = JsonSerializer.Deserialize<List<ReceiptItem>>(JsonSerializer.Serialize(receiptItemsCache));

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

            foreach (var receipt in receipts)
            {
                receipt.ReceiptItems = receiptItems.Where(x => x.ReceiptId == receipt.Id).ToList();

                foreach (var receiptItem in receipt.ReceiptItems)
                {
                    receiptItem.Resource = resources.SingleOrDefault(r => r.Id == receiptItem.ResourceId);
                    receiptItem.Unit = units.SingleOrDefault(u => u.Id == receiptItem.UnitId);
                }
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
                    Id = receipts.Count() == 0 ? 1 : receipts.Last().Id + 1,
                    Number = receipts.Count() == 0 ? 1 : receipts.Last().Number + 1,
                    Date = DateTime.Now.Date, //TODO: Datetime provider
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
            var receiptItems = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();
            var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

            try
            {
                if (ModelState.IsValid)
                {
                    //TODO: add check for existing entity id if(!_repo.Exist(id))

                    if (receiptModel.Receipt.ReceiptItems != null)
                    {
                        foreach (var item in receiptModel.Receipt.ReceiptItems) //!
                        {
                            var receiptItem = new ReceiptItem()
                            {
                                Id = receiptItems.Count() == 0 ? 1 : receiptItems.Last().Id + 1,
                                ReceiptId = receiptModel.Receipt.Id,
                                ResourceId = item.ResourceId,
                                UnitId = item.UnitId,
                                Quantity = item.Quantity
                            };

                            item.Id = receiptItem.Id;
                            item.Resource = resources.FirstOrDefault(r => r.Id == item.ResourceId);
                            item.Unit = units.FirstOrDefault(u => u.Id == item.UnitId);

                            receiptItems.Add(receiptItem);

                            var balanceCached = balances.SingleOrDefault(x => x.ResourceId == item.ResourceId && x.UnitId == item.UnitId);

                            if (balanceCached != null)
                            {
                                balanceCached.Quantity += item.Quantity;
                            }
                            else
                            {
                                var balance = new Balance()
                                {
                                    Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //test data
                                    ResourceId = item.ResourceId,
                                    UnitId = item.UnitId,
                                    Quantity = item.Quantity
                                };

                                balances.Add(balance);
                            }
                        }
                        _memoryCache.Set("Balances", balances); //
                        _memoryCache.Set("ReceiptItems", receiptItems); //
                    }
                    else
                    {
                        receiptModel.Receipt.ReceiptItems = new List<ReceiptItem>();
                    }

                    var receipt = new Receipt()
                    {
                        Id = receiptModel.Receipt.Id,
                        Number = receiptModel.Receipt.Number,
                        Date = receiptModel.Receipt.Date
                    };

                    receipts.Add(receipt); 

                    _memoryCache.Set("Receipts", receipts); //

                    TempData["Success"] = "Новое поступление успешно добавлено.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка!");
            }

            //
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
            if (id <= 0)
            {
                return NotFound();
            }
            //UoW Get
            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();

            var receipt = receipts.SingleOrDefault(x => x.Id == id);

            if (receipt == null)
            {
                return NotFound();
            }
            else
            {
                var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
                var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
                var receiptItemsCache = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();

                var receiptItems = JsonSerializer.Deserialize<List<ReceiptItem>>(JsonSerializer.Serialize(receiptItemsCache));

                var receiptModel = new Receipt()
                {
                    Id = receipt.Id,
                    Number = receipt.Number,
                    Date = receipt.Date,
                    ReceiptItems = receiptItems.Where(x => x.ReceiptId == receipt.Id).ToList() ?? new List<ReceiptItem>()
                };

                if (receiptModel.ReceiptItems.Count() != 0)
                {
                    foreach (var receiptItem in receiptModel.ReceiptItems)
                    {
                        receiptItem.Resource = resources.FirstOrDefault(r => r.Id == receiptItem.ResourceId);
                        receiptItem.Unit = units.FirstOrDefault(u => u.Id == receiptItem.UnitId);
                    }
                }

                var vm = new EditReceiptViewModel()
                {
                    Receipt = receiptModel,
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
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Receipt")] EditReceiptViewModel receiptModel) //new items don't add balances
        { 
            if (id != receiptModel.Receipt.Id || id <= 0)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
                    var rec = receipts.SingleOrDefault(r => r.Id == id);
                    if (rec is not null)
                    {
                        rec.Number = receiptModel.Receipt.Number;
                        rec.Date = receiptModel.Receipt.Date;

                        _memoryCache.Set("Receipts", receipts);
                        TempData["Success"] = "Поступление успешно отредактировано.";

                        var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
                        var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
                        var receiptItems = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();
                        var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

                        var oldReceiptItems = receiptModel.Receipt.ReceiptItems?.Where(x => x.Id != 0).ToList() ?? new List<ReceiptItem>();

                        var deletedReceitpItems = receiptItems
                            .Where(x => x.ReceiptId == receiptModel.Receipt.Id && !oldReceiptItems.Any(modelItem => modelItem.Id == x.Id))
                            .ToList() ?? new List<ReceiptItem>();

                        foreach (var item in deletedReceitpItems)
                        {
                            var balance = balances.SingleOrDefault(x => x.ResourceId == item.ResourceId && x.UnitId == item.UnitId);
                            balance.Quantity -= item.Quantity;
                            if (balance.Quantity <= 0) balances.Remove(balance);
                            receiptItems.Remove(item);
                        }

                        if (receiptModel.Receipt.ReceiptItems is not null)
                        {
                            foreach (var item in receiptModel.Receipt.ReceiptItems) //foreach .IsUpdated
                            {
                                if (item.Id == 0)
                                {
                                    var receiptItem = new ReceiptItem()
                                    {
                                        Id = receiptItems.Count() == 0 ? 1 : receiptItems.Last().Id + 1, //test data
                                        ReceiptId = receiptModel.Receipt.Id,
                                        ResourceId = item.ResourceId,
                                        UnitId = item.UnitId,
                                        Quantity = item.Quantity
                                    };

                                    item.Id = receiptItem.Id;

                                    receiptItems.Add(receiptItem);

                                    var balanceCached = balances.SingleOrDefault(b => b.ResourceId == item.ResourceId && b.UnitId == item.UnitId);

                                    if (balanceCached != null)
                                    {
                                        balanceCached.Quantity += item.Quantity;
                                    }
                                    else
                                    {
                                        var balance = new Balance()
                                        {
                                            Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //test data
                                            ResourceId = item.ResourceId,
                                            UnitId = item.UnitId,
                                            Quantity = item.Quantity
                                        };

                                        balances.Add(balance);
                                    }

                                }
                                else
                                {
                                    var existingItem = receiptItems.SingleOrDefault(ri => ri.Id == item.Id);

                                    if (existingItem is not null && item.GetHashCode() != existingItem?.GetHashCode()) //
                                    {
                                        var existingBalance = balances.SingleOrDefault(b => b.ResourceId == existingItem.ResourceId && b.UnitId == existingItem.UnitId);

                                        if (item.ResourceId != existingItem.ResourceId || item.UnitId != existingItem.UnitId)
                                        {
                                            existingItem.ResourceId = item.ResourceId;
                                            existingItem.UnitId = item.UnitId;

                                            existingBalance.Quantity -= existingItem.Quantity;

                                            if (existingBalance.Quantity <= 0) balances.Remove(existingBalance);

                                            var editedItemBalance = balances.SingleOrDefault(b => b.ResourceId == item.ResourceId && b.UnitId == item.UnitId);

                                            if (editedItemBalance != null)
                                            {
                                                editedItemBalance.Quantity += item.Quantity;
                                            }
                                            else
                                            {
                                                var balance = new Balance()
                                                {
                                                    Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //test data
                                                    ResourceId = item.ResourceId,
                                                    UnitId = item.UnitId,
                                                    Quantity = item.Quantity
                                                };

                                                balances.Add(balance);
                                            }
                                        }
                                        else
                                        {
                                            if (item.Quantity != existingItem.Quantity)
                                            {
                                                existingItem.Quantity = item.Quantity;

                                                existingBalance.Quantity -= existingItem.Quantity;
                                                existingBalance.Quantity += item.Quantity;
                                            }
                                        }
                                    }
                                }

                                item.Resource = resources.SingleOrDefault(r => r.Id == item.ResourceId);
                                item.Unit = units.SingleOrDefault(u => u.Id == item.UnitId);
                            }

                            //foreach .IsDeleted
                            _memoryCache.Set("Balances", balances);
                            _memoryCache.Set("ReceiptItems", receiptItems);
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
                }
                catch (Exception)
                {
                    throw;
                }
            }
            TempData["Error"] = "Что-то пошло не так.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();
                var receiptItemsCache = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();
                var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

                var receipt = receipts.FirstOrDefault(x => x.Id == id);

                if (receipt == null)
                {
                    return NotFound();
                }
                else
                {
                    if (receipts.Remove(receipt))
                    {
                        var receiptItems = receiptItemsCache.Where(ri => ri.ReceiptId == receipt.Id).ToList();

                        foreach (var receiptItem in receiptItems)
                        {
                            var balance = balances.SingleOrDefault(b => b.ResourceId == receiptItem.ResourceId && b.UnitId == receiptItem.UnitId);
                            balance.Quantity -= receiptItem.Quantity; //TODO: handle if an arithmetic overflow occurs
                            if (balance.Quantity <= 0) balances.Remove(balance);
                            receiptItemsCache.Remove(receiptItem);
                        }

                        _memoryCache.Set("Receipts", receipts);
                        _memoryCache.Set("ReceiptItems", receiptItemsCache);
                        _memoryCache.Set("Balances", balances);

                        TempData["Success"] = "Поступление успешно удалено.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Что-то пошло не так.";
                        return RedirectToAction(nameof(Edit), id);
                    }
                }
            }
            catch
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
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
