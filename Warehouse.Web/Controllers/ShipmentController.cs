using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Reflection.Metadata;
using System.Resources;
using System.Text.Json;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure;
using Warehouse.Web.Helpers;
using Warehouse.Web.ViewModels.Shipment;

namespace Warehouse.Web.Controllers
{
    public class ShipmentController : Controller
    {
        private readonly ILogger<ShipmentController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        public ShipmentController(ILogger<ShipmentController> logger,
            WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
        }

        public async Task<IActionResult> Index([Bind("DateRange,SelectedClients,SelectedNumbers,SelectedResources,SelectedUnits")] ShipmentsListViewModel shipmentsListView)
        {
            try
            {
                var resourcesSelectItems = await _warehouseDbContext.Resources //TODO: Filter by IsActive resource, unit, client in receipt, shipment and balance controller
                    .Select(r => new SelectListItem
                    {
                        Text = r.Title,
                        Value = r.Id.ToString()
                    })
                    .ToListAsync();

                var unitsSelectItems = await _warehouseDbContext.Units
                    .Select(u => new SelectListItem
                    {
                        Text = u.Title,
                        Value = u.Id.ToString()
                    })
                    .ToListAsync();

                var shipmentNumbersSelectItems = await _warehouseDbContext.Shipments
                    .Select(s => new SelectListItem
                    {
                        Text = s.Number.ToString(),
                        Value = s.Id.ToString()
                    })
                    .ToListAsync();

                var shipmentClientsSelectItems = await _warehouseDbContext.Clients
                    .Select(c => new SelectListItem
                    {
                        Text = c.Title,
                        Value = c.Id.ToString()
                    })
                    .ToListAsync();

                DateTime? start = null;
                DateTime? end = null;

                if (shipmentsListView.DateRange is not null)
                {
                    var dates = shipmentsListView.DateRange.Split(" - ");
                    start = DateTime.ParseExact(dates[0], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None); //TODO: Handle parsing exceptions
                    end = DateTime.ParseExact(dates[1], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                }

                var shipments = await _warehouseDbContext.Shipments
                    .Where(s => start == null || s.Date >= start)
                    .Where(s => end == null || s.Date <= end)
                    .Where(s => shipmentsListView.SelectedNumbers == null ||
                                shipmentsListView.SelectedNumbers.Count() <= 0 ||
                                shipmentsListView.SelectedNumbers.Contains(s.Id))
                    .Where(s => shipmentsListView.SelectedClients == null ||
                                shipmentsListView.SelectedClients.Count() <= 0 ||
                                shipmentsListView.SelectedClients.Contains(s.Id))
                    .Select(s => new Shipment
                    {
                        Id = s.Id,
                        Date = s.Date,
                        Number = s.Number,
                        ClientId = s.ClientId,
                        Client = new Client { Id = s.Client.Id, Title = s.Client.Title },
                        IsSigned = s.IsSigned,
                        ShipmentItems = s.ShipmentItems.Select(si => new ShipmentItem
                        {
                            Id = si.Id,
                            Quantity = si.Quantity,
                            Resource = si.Resource,
                            ResourceId = si.ResourceId,
                            Unit = si.Unit,
                            UnitId = si.UnitId
                        })
                        .ToList()
                    })
                    .ToListAsync();

                shipments = shipments
                    .Where(s => shipmentsListView.SelectedResources == null ||
                                shipmentsListView.SelectedResources.Count() <= 0 ||
                                s.ShipmentItems.Any(si => shipmentsListView.SelectedResources.Contains(si.ResourceId)))
                    .Where(s => shipmentsListView.SelectedUnits == null ||
                                shipmentsListView.SelectedUnits.Count() <= 0 ||
                                s.ShipmentItems.Any(ri => shipmentsListView.SelectedUnits.Contains(ri.UnitId)))
                    .ToList();

                var vm = new ShipmentsListViewModel()
                {
                    Shipments = shipments,
                    ShipmentsNumbers = shipmentNumbersSelectItems,
                    Clients = shipmentClientsSelectItems,
                    Resources = resourcesSelectItems,
                    Units = unitsSelectItems,
                    DateRange = shipmentsListView.DateRange ?? "01-01-2025 - 01-01-2027",
                    SelectedNumbers = shipmentsListView.SelectedNumbers ?? new List<int>(),
                    SelectedClients = shipmentsListView.SelectedClients ?? new List<int>(),
                    SelectedResources = shipmentsListView.SelectedResources ?? new List<int>(),
                    SelectedUnits = shipmentsListView.SelectedUnits ?? new List<int>()
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

            var shipmentNumbersSelectItems = await _warehouseDbContext.Shipments
                .Select(r => new SelectListItem
                {
                    Text = r.Number.ToString(),
                    Value = r.Id.ToString()
                })
                .ToListAsync();

            var shipmentClientsSelectItems = await _warehouseDbContext.Clients
                .Select(c => new SelectListItem
                {
                    Text = c.Title,
                    Value = c.Id.ToString()
                })
                .ToListAsync();

            var vm = new CreateShipmentViewModel()
            {
                Shipment = new Shipment()
                {
                    Id = 0,
                    Number = shipmentNumbersSelectItems.Count == 0 ? 1 : Int32.Parse(shipmentNumbersSelectItems.Last().Text) + 1,
                    Date = DateTime.Now.Date, //TODO: Datetime provider
                    ShipmentItems = new List<ShipmentItem>()
                },
                Clients = shipmentClientsSelectItems,
                Resources = resourcesSelectItems,
                Units = unitsSelectItems
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Shipment")] CreateShipmentViewModel shipmentModel)
        {
            if (ModelState.IsValid)
            {
                try //TODO: Add date validation (11-10-20266)
                {
                    if (shipmentModel.Shipment.ShipmentItems is null || shipmentModel.Shipment.ShipmentItems.Count == 0)
                    {
                        TempData["Error"] = "Отгрузка не может быть пустой.";
                        return RedirectToAction(nameof(Create)); //TODO: Return Create View with model to edit?
                    }

                    if (shipmentModel.Shipment.IsSigned)
                    {
                        var shipmentItems = shipmentModel.Shipment.ShipmentItems.ToList();
                        var isInvalidBalance = await BalanceHelper.IsUnableToSignShipment(_warehouseDbContext, shipmentItems);

                        if (isInvalidBalance)
                        {
                            TempData["Error"] = $"Не возможно подписать отгрузку: недостаточный баланс."; //TODO: invalid balance info. Create result class
                            return RedirectToAction(nameof(Index)); //TODO: Create unsigned shipment and return Create View for edit?
                        }

                        BalanceHelper.UpdateBalanceBySigningShipment(_warehouseDbContext, shipmentItems);

                        _warehouseDbContext.Shipments.Add(shipmentModel.Shipment);
                        await _warehouseDbContext.SaveChangesAsync();

                        TempData["Success"] = "Новая отгрузка успешно добавлена и подписана.";
                        return RedirectToAction(nameof(Index));
                    }

                    _warehouseDbContext.Shipments.Add(shipmentModel.Shipment);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Новая отгрузка успешно добавлена.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    throw;
                }
            }

            TempData["Error"] = "Ошибка валидации.";

            var resourcesSelectItems = await _warehouseDbContext.Resources //TODO: create method for returning selectitems?
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

            var shipmentClientsSelectItems = await _warehouseDbContext.Clients
                .Select(c => new SelectListItem
                {
                    Text = c.Title,
                    Value = c.Id.ToString()
                })
                .ToListAsync();

            shipmentModel.Shipment.ShipmentItems ??= new List<ShipmentItem>();
            shipmentModel.Resources = resourcesSelectItems;
            shipmentModel.Units = unitsSelectItems;
            shipmentModel.Clients = shipmentClientsSelectItems;

            return View(shipmentModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Не верный идентификатор.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var shipment = await _warehouseDbContext.Shipments
                    .Include(s => s.ShipmentItems)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (shipment is null)
                {
                    TempData["Error"] = "Отгрузка не найдена.";
                    return RedirectToAction(nameof(Index));
                }

                if (shipment.IsSigned)
                {
                    return RedirectToAction(nameof(EditSigned), new { id = shipment.Id });
                }

                var shipmentModel = new Shipment() //TODO: service should return dto
                {
                    Id = shipment.Id,
                    Number = shipment.Number,
                    ClientId = shipment.ClientId,
                    Date = shipment.Date,
                    IsSigned = shipment.IsSigned,
                    ShipmentItems = shipment.ShipmentItems
                };

                var resourcesSelectItems = await _warehouseDbContext.Resources //TODO: create method for returning selectitems? Cache results.
                    .Where(r => r.IsActive)
                    .Select(r => new SelectListItem
                    {
                        Text = r.Title.ToString(),
                        Value = r.Id.ToString()
                    })
                    .ToListAsync();

                var unitsSelectItems = await _warehouseDbContext.Units
                    .Where(u => u.IsActive)
                    .Select(u => new SelectListItem
                    {
                        Text = u.Title.ToString(),
                        Value = u.Id.ToString()
                    })
                    .ToListAsync();

                var shipmentClientsSelectItems = await _warehouseDbContext.Clients
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Text = c.Title,
                        Value = c.Id.ToString()
                    })
                    .ToListAsync();

                var vm = new EditShipmentViewModel()
                {
                    Shipment = shipmentModel,
                    Clients = shipmentClientsSelectItems,
                    Resources = resourcesSelectItems,
                    Units = unitsSelectItems
                };

                return View(vm);
            }
            catch (Exception)
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> EditSigned(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Не верный идентификатор.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var shipment = await _warehouseDbContext.Shipments
                    .Include(s => s.Client)
                    .Include(s => s.ShipmentItems)
                        .ThenInclude(si => si.Resource)
                    .Include(s => s.ShipmentItems)
                        .ThenInclude(si => si.Unit)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (shipment is null)
                {
                    TempData["Error"] = "Отгрузка не найдена.";
                    return RedirectToAction(nameof(Index));
                }

                var vm = new EditShipmentViewModel()
                {
                    Shipment = shipment,
                };

                return View(vm);
            }
            catch (Exception)
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Shipment")] EditShipmentViewModel shipmentModel)
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
                if (id != shipmentModel.Shipment.Id || id <= 0)
                {
                    BadRequest("Неверный идентификатор"); //TODO: Error view
                }

                if (shipmentModel.Shipment.ShipmentItems is null || !shipmentModel.Shipment.ShipmentItems.Any())
                {
                    TempData["Error"] = "Отгрузка не может быть пустой.";
                    return RedirectToAction(nameof(Create)); //TODO: Return Edit View with model to edit?
                }

                try
                {
                    var numberExists = await _warehouseDbContext.Shipments
                        .AnyAsync(s => s.Id != shipmentModel.Shipment.Id && s.Number == shipmentModel.Shipment.Number);

                    if (numberExists)
                    {
                        TempData["Error"] = "Уже существует отгрузка с таким номером.";
                        return RedirectToAction(nameof(Index)); //TODO: Return View(model) for continue editing
                    }

                    var entity = await _warehouseDbContext.Shipments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (entity == null)
                    {
                        TempData["Error"] = "Отгрузка не найдена.";
                        return RedirectToAction(nameof(Index));
                    }

                    var previousItems = await _warehouseDbContext.ShipmentItems
                        .Where(si => si.ShipmentId == shipmentModel.Shipment.Id)
                        .AsNoTracking()
                        .ToListAsync();

                    var newItems = shipmentModel.Shipment.ShipmentItems.ToList();

                    if (!entity.IsSigned && shipmentModel.Shipment.IsSigned)
                    {
                        var isInvalidBalance = await BalanceHelper.IsUnableToSignShipment(_warehouseDbContext, newItems);

                        if (isInvalidBalance)
                        {
                            TempData["Error"] = "Невозможно подписать отгрузку: недостаточный баланс.";
                            return RedirectToAction(nameof(Edit), new { id });
                        }

                        BalanceHelper.UpdateBalanceBySigningShipment(_warehouseDbContext, newItems);

                        TempData["Success"] = "Отгрузка успешно подписана.";
                    }                
                    
                    var shipmentItemsToDelete = previousItems
                        .Where(si => !shipmentModel.Shipment.ShipmentItems.Any(i => i.Id == si.Id));

                    _warehouseDbContext.ShipmentItems.RemoveRange(shipmentItemsToDelete);
                    _warehouseDbContext.Shipments.Update(shipmentModel.Shipment);

                    await _warehouseDbContext.SaveChangesAsync();
                    TempData["Success"] += "Отгрузка успешно отредактирована.";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception) //TODO: Handle exceptions
                {
                    throw;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Unsign(int id)
        {
            if (id <= 0)
            {
                return NotFound(); //
            }

            try
            {
                var entity = await _warehouseDbContext.Shipments
                    .Include(e => e.ShipmentItems)
                    .FirstOrDefaultAsync(entity => entity.Id == id);

                if (entity == null)
                {
                    TempData["Error"] = "Отгрузка не найдена.";
                    return RedirectToAction(nameof(Index));
                }

                if (entity.IsSigned)
                {
                    entity.IsSigned = false;
                    _warehouseDbContext.Shipments.Update(entity);

                    BalanceHelper.UpdateBalanceByUnsigningShipment(_warehouseDbContext, entity.ShipmentItems.ToList());

                    TempData["Success"] = "Отгрузка успешно отозвана.";
                    await _warehouseDbContext.SaveChangesAsync();
                }
                else
                {
                    TempData["Error"] = "Что-то пошло не так.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {

                throw;
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
                var entity = await _warehouseDbContext.Shipments
                    .Include(e => e.ShipmentItems)
                    .FirstOrDefaultAsync(entity => entity.Id == id);

                if (entity == null)
                {
                    TempData["Error"] = "Отгрузка не найдена.";
                    return RedirectToAction(nameof(Index));
                }

                if (entity.IsSigned)
                {
                    TempData["Error"] = "Не возможно удалить подписанную отгрузку.";
                    return RedirectToAction(nameof(Index));
                }

                _warehouseDbContext.Shipments.Remove(entity);
                await _warehouseDbContext.SaveChangesAsync();

                TempData["Success"] = "Отгрузка успешно удалена.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
