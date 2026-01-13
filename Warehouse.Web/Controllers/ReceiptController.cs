using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Warehouse.Domain.Entities;
using Warehouse.Web.ViewModels.Receipt;
using System.Text.Json;
using Warehouse.Web.Helpers;
using Warehouse.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Resources;

namespace Warehouse.Web.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly ILogger<ReceiptController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        private readonly IMemoryCache _memoryCache;

        public ReceiptController(ILogger<ReceiptController> logger, 
            WarehouseDbContext warehouseDbContext,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
            _memoryCache = memoryCache; 
        }

        public async Task<IActionResult> Index([Bind("DateRange,SelectedNumbers,SelectedResources,SelectedUnits")] ReceiptsListViewModel receiptsListView)
        {
            try
            {
                var resourcesSelectItems = await _warehouseDbContext.Resources
                    .Select(r => new SelectListItem
                    {
                        Text = r.Title.ToString(),
                        Value = r.Id.ToString()
                    })
                    .ToListAsync();

                var unitsSelectItems = await _warehouseDbContext.Units
                    .Select(u => new SelectListItem
                    {
                        Text = u.Title.ToString(),
                        Value = u.Id.ToString()
                    })
                    .ToListAsync();

                var receiptNumbersSelectItems = await _warehouseDbContext.Receipts
                    .Select(r => new SelectListItem
                    {
                        Text = r.Number.ToString(),
                        Value = r.Id.ToString()
                    })
                    .ToListAsync();

                DateTime? start = null;
                DateTime? end = null;

                if (receiptsListView.DateRange is not null)
                {
                    var dates = receiptsListView.DateRange.Split(" - ");
                    start = DateTime.ParseExact(dates[0], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None); //TODO: Handle parsing exceptions
                    end = DateTime.ParseExact(dates[1], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                }

                var receipts = await _warehouseDbContext.Receipts
                    .Where(r => start == null || r.Date >= start)
                    .Where(r => end == null || r.Date <= end)
                    .Where(r => receiptsListView.SelectedNumbers == null ||
                                receiptsListView.SelectedNumbers.Count() <= 0 ||
                                receiptsListView.SelectedNumbers.Contains(r.Id))
                    .Select(r => new Receipt
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Number = r.Number,
                        ReceiptItems = r.ReceiptItems.Select(ri => new ReceiptItem
                        {
                            Id = ri.Id,
                            Quantity = ri.Quantity,
                            Resource = ri.Resource,
                            ResourceId = ri.ResourceId,
                            Unit = ri.Unit,
                            UnitId = ri.UnitId
                        })
                        .ToList()
                    })
                    .ToListAsync();

                receipts = receipts
                    .Where(r => receiptsListView.SelectedResources == null ||
                                receiptsListView.SelectedResources.Count() <= 0 ||
                                r.ReceiptItems.Any(ri => receiptsListView.SelectedResources.Contains(ri.ResourceId)))
                    .Where(r => receiptsListView.SelectedUnits == null ||
                                receiptsListView.SelectedUnits.Count() <= 0 ||
                                r.ReceiptItems.Any(ri => receiptsListView.SelectedUnits.Contains(ri.UnitId)))
                    .ToList();

                var vm = new ReceiptsListViewModel()
                {
                    Receipts = receipts,
                    ReceiptNumbers = receiptNumbersSelectItems,
                    Resources = resourcesSelectItems,
                    Units = unitsSelectItems,
                    DateRange = receiptsListView.DateRange ?? "01-01-2025 - 01-01-2027",
                    SelectedNumbers = receiptsListView.SelectedNumbers ?? new List<int>(),
                    SelectedResources = receiptsListView.SelectedResources ?? new List<int>(),
                    SelectedUnits = receiptsListView.SelectedUnits ?? new List<int>()
                };

                return View(vm);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IActionResult> Create()
        {
            var resourcesSelectItems = await _warehouseDbContext.Resources
                .Select(r => new SelectListItem
                {
                    Text = r.Title.ToString(),
                    Value = r.Id.ToString()
                })
                .ToListAsync();

            var unitsSelectItems = await _warehouseDbContext.Units
                .Select(u => new SelectListItem
                {
                    Text = u.Title.ToString(),
                    Value = u.Id.ToString()
                })
                .ToListAsync();

            var receiptNumbersSelectItems = await _warehouseDbContext.Receipts
                .Select(r => new SelectListItem
                {
                    Text = r.Number.ToString(),
                    Value = r.Id.ToString()
                })
                .ToListAsync();

            var vm = new CreateReceiptViewModel()
            {
                Receipt = new Receipt()
                {
                    Id = 0,
                    Number = receiptNumbersSelectItems is null ? 0 : Int32.Parse(receiptNumbersSelectItems.Last().Text) + 1,
                    Date = DateTime.Now.Date, //TODO: Datetime provider
                    ReceiptItems = new List<ReceiptItem>()
                },
                Resources = resourcesSelectItems,
                Units = unitsSelectItems
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Receipt")] CreateReceiptViewModel receiptModel)
        {
            if (ModelState.IsValid) //TODO: Add date validator
            {
                try
                {
                    var receipts = await _warehouseDbContext.Receipts.ToListAsync(); //TODO: useless query

                    if (receiptModel.Receipt.ReceiptItems != null && receiptModel.Receipt.ReceiptItems.Count > 0)
                    {
                        foreach (var item in receiptModel.Receipt.ReceiptItems)
                        {

                            var balance = await _warehouseDbContext.Balances.FirstOrDefaultAsync(x => x.ResourceId == item.ResourceId && x.UnitId == item.UnitId);

                            if (balance != null)
                            {
                                balance.Quantity += item.Quantity;
                                _warehouseDbContext.Balances.Update(balance);
                            }
                            else
                            {
                                var newBalance = new Balance()
                                {
                                    ResourceId = item.ResourceId,
                                    UnitId = item.UnitId,
                                    Quantity = item.Quantity
                                };

                                _warehouseDbContext.Balances.Add(newBalance);
                            }
                        }
                    }

                    _warehouseDbContext.Receipts.Add(receiptModel.Receipt);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Новое поступление успешно добавлено.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Произошла ошибка!");
                    throw;
                }
            }

            TempData["Error"] = "Ошибка валидации.";

            var resourcesSelectItems = await _warehouseDbContext.Resources
                .Select(r => new SelectListItem
                {
                    Text = r.Title.ToString(),
                    Value = r.Id.ToString()
                })
                .ToListAsync();

            var unitsSelectItems = await _warehouseDbContext.Units
                .Select(u => new SelectListItem
                {
                    Text = u.Title.ToString(),
                    Value = u.Id.ToString()
                })
                .ToListAsync();

            receiptModel.Resources = resourcesSelectItems;
            receiptModel.Units = unitsSelectItems;

            return View(receiptModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var receipt = await _warehouseDbContext.Receipts
                    .Include(r => r.ReceiptItems)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (receipt == null)
                {
                    return NotFound();
                }

                var resourcesSelectItems = await _warehouseDbContext.Resources
                    .Select(r => new SelectListItem
                    {
                        Text = r.Title.ToString(),
                        Value = r.Id.ToString()
                    })
                    .ToListAsync();

                var unitsSelectItems = await _warehouseDbContext.Units
                    .Select(u => new SelectListItem
                    {
                        Text = u.Title.ToString(),
                        Value = u.Id.ToString()
                    })
                    .ToListAsync();

                var vm = new EditReceiptViewModel()
                {
                    Receipt = receipt, //TODO: create dtos
                    Resources = resourcesSelectItems,
                    Units = unitsSelectItems
                };

                return View(vm);
            }
            catch (Exception)
            {
                throw;
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

                return BadRequest(new { Errors = errors }); //TODO: Error view
            }
            else
            {
                if (id != receiptModel.Receipt.Id || id <= 0)
                {
                    BadRequest("Неверный идентификатор"); //TODO: Error view
                }

                try
                {
                    var numberExists = await _warehouseDbContext.Receipts
                        .AnyAsync(r => r.Id != receiptModel.Receipt.Id && r.Number == receiptModel.Receipt.Number);

                    if (numberExists)
                    {
                        TempData["Error"] = "Уже существует поступление с таким номером.";
                        return RedirectToAction(nameof(Index));
                    }

                    var previousItems = await _warehouseDbContext.ReceiptItems
                        .Where(ri => ri.ReceiptId == receiptModel.Receipt.Id)
                        .AsNoTracking()
                        .ToListAsync();

                    if (receiptModel.Receipt.ReceiptItems == null)
                    {
                        receiptModel.Receipt.ReceiptItems = new List<ReceiptItem>();
                    }

                    var newItems = receiptModel.Receipt.ReceiptItems.ToList();

                    var isInvalidBalance = await BalanceHelper.IsInvalidReceiptUpdate(_warehouseDbContext, previousItems, newItems);

                    if (isInvalidBalance)
                    {
                        TempData["Error"] = "Невозможно отредактировать поступление: недостаточный баланс.";
                        return RedirectToAction(nameof(Edit), new { id });
                    }

                    var recieptItemsToDelete = previousItems
                        .Where(ri => !receiptModel.Receipt.ReceiptItems.Any(i => i.Id == ri.Id));

                    _warehouseDbContext.ReceiptItems.RemoveRange(recieptItemsToDelete);
                    _warehouseDbContext.Receipts.Update(receiptModel.Receipt);

                    BalanceHelper.UpdateBalanceFromReceipt(_warehouseDbContext, previousItems, newItems);

                    await _warehouseDbContext.SaveChangesAsync();
                    TempData["Success"] = "Поступление успешно отредактировано.";

                    receiptModel.Resources = await _warehouseDbContext.Resources
                        .Select(r => new SelectListItem
                        {
                            Text = r.Title.ToString(),
                            Value = r.Id.ToString()
                        })
                        .ToListAsync();

                    receiptModel.Units = await _warehouseDbContext.Units
                        .Select(u => new SelectListItem
                        {
                            Text = u.Title.ToString(),
                            Value = u.Id.ToString()
                        })
                        .ToListAsync();

                    return View(receiptModel);
                }
                catch (Exception) //TODO: Handle exceptions
                {
                    throw;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound(); //
            }

            try
            {
                var entity = await _warehouseDbContext.Receipts
                    .Include(e => e.ReceiptItems)
                    .FirstOrDefaultAsync(entity => entity.Id == id);

                if (entity == null)
                {
                    return NotFound("Не удалось найти предоставленный идентификатор."); //
                }

                if (await BalanceHelper.IsInvalidReceiptDeletion(_warehouseDbContext, id))
                {
                    TempData["Error"] = "Невозможно удалить поступление: недостаточный баланс.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                _warehouseDbContext.Receipts.Remove(entity);

                BalanceHelper.UpdateBalanceFromReceipt(
                    _warehouseDbContext,
                    entity.ReceiptItems.ToList(),
                    new List<ReceiptItem>()
                );

                await _warehouseDbContext.SaveChangesAsync();

                TempData["Success"] = "Поступление успешно удалено.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Что-то пошло не так. Ошибка: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
