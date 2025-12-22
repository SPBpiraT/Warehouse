using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Receipt;
using System.Text.Json;
using Warehouse.Web.Helpers;
using System.Resources;

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

            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>();

            var receipt = receipts.SingleOrDefault(x => x.Id == id); //TODO: handle ex

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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { Errors = errors });
            }

            if (id != receiptModel.Receipt.Id || id <= 0)
            {
                BadRequest("Неверный идентификатор");
            }

            var receipts = _memoryCache.Get<List<Receipt>>("Receipts") ?? new List<Receipt>(); //context
            var entity = receipts.FirstOrDefault(r => r.Id != receiptModel.Receipt.Id && r.Number == receiptModel.Receipt.Number); //await context

            if (entity != null)
            {
                return BadRequest("Уже существует поступление с таким же номером.");
            }

            var receiptItems = _memoryCache.Get<List<ReceiptItem>>("ReceiptItems") ?? new List<ReceiptItem>(); //context
            var previousItems = receiptItems.Where(ri => ri.ReceiptId == receiptModel.Receipt.Id).ToList(); //await context
            var newItems = receiptModel.Receipt.ReceiptItems?.ToList() ?? new List<ReceiptItem>();

            var isInvalidBalance = await BalanceHelper.IsInvalidReceiptUpdate(_memoryCache, previousItems, newItems);

            if (isInvalidBalance)
            {
                TempData["Error"] = "Невозможно отредактировать поступление: недостаточный баланс.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            receiptItems.RemoveAll(ri => ri.ReceiptId == receiptModel.Receipt.Id);

            if (receiptModel.Receipt.ReceiptItems is not null && receiptModel.Receipt.ReceiptItems.Count > 0)
            {
                foreach (var item in receiptModel.Receipt.ReceiptItems)
                {
                    if (item.Id == 0)
                    {
                        item.Id = receiptItems.Count() == 0 ? 1 : receiptItems.Last().Id + 1; //test data
                    }

                    receiptItems.Add(item);
                }
            }

            var oldReceipt = receipts.FirstOrDefault(r => r.Id == receiptModel.Receipt.Id);
            receipts.Remove(oldReceipt);

            var newReceipt = new Receipt()
            {
                Id = receiptModel.Receipt.Id,
                Number = receiptModel.Receipt.Number,
                Date = receiptModel.Receipt.Date
            };

            receipts.Add(newReceipt);

            BalanceHelper.UpdateBalanceFromReceipt(_memoryCache, previousItems, newItems);

            TempData["Success"] = "Поступление успешно отредактировано.";

            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

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
