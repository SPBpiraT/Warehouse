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

            if (receiptsListView.SelectedNumbers != null && receiptsListView.SelectedNumbers.Any())
            {
                receipts = receipts.Where(r => receiptsListView.SelectedNumbers.Contains(r.Id)).ToList();
            }

            if (receiptsListView.SelectedResources != null && receiptsListView.SelectedResources.Any())
            {
                var filteredReceiptIds = receiptItems
                    .Where(ri => receiptsListView.SelectedResources.Contains(ri.ResourceId))
                    .Select(ri => ri.ReceiptId)
                    .Distinct()
                    .ToList();

                receipts = receipts
                    .Where(r => filteredReceiptIds.Contains(r.Id))
                    .ToList();
            }

            if (receiptsListView.SelectedUnits != null && receiptsListView.SelectedUnits.Any())
            {
                var filteredReceiptIds = receiptItems
                    .Where(ri => receiptsListView.SelectedUnits.Contains(ri.UnitId))
                    .Select(ri => ri.ReceiptId)
                    .Distinct()
                    .ToList();

                receipts = receipts
                    .Where(r => filteredReceiptIds.Contains(r.Id))
                    .ToList();
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
        public async Task<IActionResult> Edit(int id, [Bind("Receipt")] EditReceiptViewModel receiptModel)
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
                    var receipt = receipts.SingleOrDefault(r => r.Id == id);

                    if (receipt is null)
                    {
                        return NotFound();
                    }

                    receipt.Number = receiptModel.Receipt.Number;
                    receipt.Date = receiptModel.Receipt.Date;

                    var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
                    var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
                    var receiptItems = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>();
                    var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

                    var oldReceiptItems = receiptModel.Receipt.ReceiptItems?.Where(x => x.Id != 0).ToList() ?? new List<ReceiptItem>();

                    //TODO: Add to ViewModel props for Added, Changed and Deleted receipt items ids
                    var changedReceiptItems = receiptItems
                        .Where(x => x.ReceiptId == receiptModel.Receipt.Id && 
                                (
                                    !oldReceiptItems.Any(modelItem => modelItem.Id == x.Id) ||
                                    oldReceiptItems.Any(modelItem => modelItem.Id == x.Id && (modelItem.ResourceId != x.ResourceId || modelItem.UnitId != x.UnitId)) ||
                                    oldReceiptItems.Any(modelItem => modelItem.Id == x.Id && (modelItem.ResourceId == x.ResourceId && modelItem.UnitId == x.UnitId && modelItem.Quantity != x.Quantity))
                                )
                              )
                        .ToList() ?? new List<ReceiptItem>();

                    var changedItemsBalanceChecks = changedReceiptItems
                            .Select(ri => new
                            {
                                StoredReceiptItem = ri,
                                ModelReceiptItem = oldReceiptItems.FirstOrDefault(x => x.Id == ri.Id),
                                Balance = balances.SingleOrDefault(b => b.ResourceId == ri.ResourceId && b.UnitId == ri.UnitId),
                                ChangeType = oldReceiptItems.Any(ori => ori.Id == ri.Id && (ori.ResourceId != ri.ResourceId || ori.UnitId != ri.UnitId)) 
                                    ? "ResourceOrUnitUpdate" :
                                oldReceiptItems.Any(ori => ori.Id == ri.Id && ori.ResourceId == ri.ResourceId && ori.UnitId == ri.UnitId && ori.Quantity != ri.Quantity) 
                                    ? "QuantityUpdate" : "Deleted",
                                BalanceInfo = $"ResourceId: {ri.ResourceId}, UnitId: {ri.UnitId}"
                            })
                            .ToList();

                    //TODO: Create Balance check helper
                    if (changedItemsBalanceChecks.Any())
                    {
                        var onlyQuantityChangedChecks = changedItemsBalanceChecks
                            .Where(x => x.ChangeType == "QuantityUpdate")
                            .ToList();

                        var deletedOrChangedResourceChecks = changedItemsBalanceChecks
                            .Where(x => x.ChangeType == "Deleted" || x.ChangeType == "ResourceOrUnitUpdate")
                            .ToList();

                        var insufficientBalance = onlyQuantityChangedChecks
                            .Where(x => x.ModelReceiptItem?.Quantity < x.StoredReceiptItem.Quantity)
                            .FirstOrDefault(x => x.Balance == null || x.Balance.Quantity < (x.StoredReceiptItem.Quantity - x.ModelReceiptItem?.Quantity)) ??
                            deletedOrChangedResourceChecks.FirstOrDefault(x => x.Balance == null || x.Balance.Quantity < x.StoredReceiptItem.Quantity);

                        if (insufficientBalance != null)
                        {
                            TempData["Error"] = $"Невозможно отредактировать поступление: недостаточный баланс {insufficientBalance.BalanceInfo}";
                            return RedirectToAction(nameof(Edit), new { id });
                        }

                        foreach (var check in changedItemsBalanceChecks)
                        {
                            if (check.ChangeType == "Deleted")
                            {
                                var balance = check.Balance;
                                balance.Quantity -= check.StoredReceiptItem.Quantity;

                                if (balance.Quantity == 0)
                                    balances.Remove(balance);

                                receiptItems.Remove(check.StoredReceiptItem);
                            }

                            if (check.ChangeType == "ResourceOrUnitUpdate")
                            {
                                var balance = check.Balance;
                                balance.Quantity -= check.StoredReceiptItem.Quantity;

                                if (balance.Quantity == 0)
                                    balances.Remove(balance);

                                check.StoredReceiptItem.ResourceId = check.ModelReceiptItem.ResourceId;
                                check.StoredReceiptItem.UnitId = check.ModelReceiptItem.UnitId;

                                var editedItemBalance = balances.SingleOrDefault(b => b.ResourceId == check.ModelReceiptItem.ResourceId && b.UnitId == check.ModelReceiptItem.UnitId);

                                if (editedItemBalance != null)
                                {
                                    editedItemBalance.Quantity += check.ModelReceiptItem.Quantity;
                                }
                                else
                                {
                                    var newBalance = new Balance()
                                    {
                                        Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //test data
                                        ResourceId = check.ModelReceiptItem.ResourceId,
                                        UnitId = check.ModelReceiptItem.UnitId,
                                        Quantity = check.ModelReceiptItem.Quantity
                                    };

                                    balances.Add(newBalance);
                                }
                            }

                            if (check.ChangeType == "QuantityUpdate")
                            {
                                ulong difference;

                                if (check.ModelReceiptItem.Quantity > check.StoredReceiptItem.Quantity)
                                {
                                    difference = check.ModelReceiptItem.Quantity - check.StoredReceiptItem.Quantity;
                                    check.Balance.Quantity += difference;
                                }
                                else
                                {
                                    difference = check.StoredReceiptItem.Quantity - check.ModelReceiptItem.Quantity;

                                    check.Balance.Quantity -= difference;

                                    if (check.Balance.Quantity == 0) balances.Remove(check.Balance);
                                }

                                check.StoredReceiptItem.Quantity = check.ModelReceiptItem.Quantity;
                            }
                        }
                    }

                    if (receiptModel.Receipt.ReceiptItems is not null)
                    {
                        foreach (var item in receiptModel.Receipt.ReceiptItems)
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

                                var existingBalance = balances.SingleOrDefault(b => b.ResourceId == item.ResourceId && b.UnitId == item.UnitId);

                                if (existingBalance != null)
                                {
                                    existingBalance.Quantity += item.Quantity;
                                }
                                else
                                {
                                    var newBalance = new Balance()
                                    {
                                        Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //test data
                                        ResourceId = item.ResourceId,
                                        UnitId = item.UnitId,
                                        Quantity = item.Quantity
                                    };

                                    balances.Add(newBalance);
                                }
                            }

                            item.Resource = resources.SingleOrDefault(r => r.Id == item.ResourceId);
                            item.Unit = units.SingleOrDefault(u => u.Id == item.UnitId);
                        }

                        _memoryCache.Set("Balances", balances); //Удалить.
                        _memoryCache.Set("Receipts", receipts);
                        _memoryCache.Set("ReceiptItems", receiptItems);
                    }

                    TempData["Success"] = "Поступление успешно отредактировано.";

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
                catch (InvalidOperationException)
                {
                    TempData["Error"] = "Обнаружены дублирующиеся балансы. Обратитесь к администратору.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["Error"] = "Неизвестная ошибка.";
                    return RedirectToAction(nameof(Edit), new { id });
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

                var relatedReceiptItems = receiptItemsCache.Where(ri => ri.ReceiptId == receipt.Id).ToList();

                var balanceChecks = relatedReceiptItems
                    .Select(ri => new
                    {
                        ReceiptItem = ri,
                        Balance = balances.FirstOrDefault(b => b.ResourceId == ri.ResourceId && b.UnitId == ri.UnitId)
                    })
                    .ToList();

                var insufficientBalance = balanceChecks.FirstOrDefault(x =>
                    x.Balance == null || x.Balance.Quantity < x.ReceiptItem.Quantity);

                if (insufficientBalance != null)
                {
                    TempData["Error"] = "Невозможно удалить поступление: недостаточный баланс.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (receipts.Remove(receipt))
                {
                    foreach (var check in balanceChecks)
                    {
                        var balance = check.Balance;
                        balance.Quantity -= check.ReceiptItem.Quantity;

                        if (balance.Quantity == 0)
                            balances.Remove(balance);

                        receiptItemsCache.Remove(check.ReceiptItem);
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
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = "Обнаружены дублирующиеся балансы. Обратитесь к администратору.";
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                TempData["Error"] = $"Что-то пошло не так. Ошибка: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
