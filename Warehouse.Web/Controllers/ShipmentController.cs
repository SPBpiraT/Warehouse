using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Receipt;
using Warehouse.Domain.ViewModels.Shipment;

namespace Warehouse.Web.Controllers
{
    public class ShipmentController : Controller
    {
        private readonly ILogger<ShipmentController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ShipmentController(ILogger<ShipmentController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index([Bind("DateRange,SelectedClients,SelectedNumbers,SelectedResources,SelectedUnits")] ShipmentsListViewModel shipmentsListView)
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
            var clients = _memoryCache.Get<List<Client>>("Clients")?.Where(u => u.IsActive) ?? new List<Client>();
            var shipmentsCache = _memoryCache.Get<List<Shipment>>("Shipments") ?? new List<Shipment>();
            var shipmentItemsCache = _memoryCache.Get<List<ShipmentItem>>("ShipmentItems") ?? new List<ShipmentItem>();

            var shipments = JsonSerializer.Deserialize<List<Shipment>>(JsonSerializer.Serialize(shipmentsCache));
            var shipmentItems = JsonSerializer.Deserialize<List<ShipmentItem>>(JsonSerializer.Serialize(shipmentItemsCache));

            if (shipmentsListView.DateRange is not null)
            {
                var dates = shipmentsListView.DateRange.Split(" - ");

                if (DateTime.TryParseExact(dates[0], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) &&
                    DateTime.TryParseExact(dates[1], "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
                {
                    shipments = shipments.Where(r => r.Date >= start && r.Date <= end).ToList();
                }
                else
                {
                    //TODO: Add error handle
                }
            }

            if (shipmentsListView.SelectedNumbers != null && shipmentsListView.SelectedNumbers.Any())
            {
                shipments = shipments.Where(r => shipmentsListView.SelectedNumbers.Contains(r.Id)).ToList();
            }

            if (shipmentsListView.SelectedClients != null && shipmentsListView.SelectedClients.Any())
            {
                shipments = shipments.Where(r => shipmentsListView.SelectedClients.Contains(r.Id)).ToList();
            }

            if (shipmentsListView.SelectedResources != null && shipmentsListView.SelectedResources.Any())
            {
                var filteredShipmentIds = shipmentItems
                    .Where(si => shipmentsListView.SelectedResources.Contains(si.ResourceId))
                    .Select(si => si.ShipmentId)
                    .Distinct()
                    .ToList();

                shipments = shipments
                    .Where(s => filteredShipmentIds.Contains(s.Id))
                    .ToList();
            }

            if (shipmentsListView.SelectedUnits != null && shipmentsListView.SelectedUnits.Any())
            {
                var filteredShipmentIds = shipmentItems
                    .Where(si => shipmentsListView.SelectedUnits.Contains(si.UnitId))
                    .Select(si => si.ShipmentId)
                    .Distinct()
                    .ToList();

                shipments = shipments
                    .Where(s => filteredShipmentIds.Contains(s.Id))
                    .ToList();
            }

            foreach (var shipment in shipments)
            {
                shipment.Client = clients.SingleOrDefault(c => c.Id == shipment.ClientId);
                shipment.ShipmentItems = shipmentItems.Where(x => x.ShipmentId == shipment.Id).ToList();

                foreach (var shipmentItem in shipment.ShipmentItems)
                {
                    shipmentItem.Resource = resources.SingleOrDefault(r => r.Id == shipmentItem.ResourceId);
                    shipmentItem.Unit = units.SingleOrDefault(u => u.Id == shipmentItem.UnitId);
                }
            }

            var vm = new ShipmentsListViewModel()
            {
                Shipments = shipments,
                ShipmentsNumbers = shipmentsCache.Select(s => new SelectListItem
                {
                    Text = s.Number.ToString(),
                    Value = s.Id.ToString()
                }),
                Clients = clients.Select(c => new SelectListItem
                {
                    Text = c.Title.ToString(),
                    Value = c.Id.ToString()
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
                DateRange = shipmentsListView.DateRange ?? "01-01-2024 - 01-01-2025",
                SelectedNumbers = shipmentsListView.SelectedNumbers ?? new List<int>(),
                SelectedClients = shipmentsListView.SelectedClients ?? new List<int>(),
                SelectedResources = shipmentsListView.SelectedResources ?? new List<int>(),
                SelectedUnits = shipmentsListView.SelectedUnits ?? new List<int>()
            };
            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            var shipments = _memoryCache.Get<List<Shipment>>("Shipments") ?? new List<Shipment>();
            var clients = _memoryCache.Get<List<Client>>("Clients")?.Where(u => u.IsActive) ?? new List<Client>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();

            var vm = new CreateShipmentViewModel()
            {
                Shipment = new Shipment()
                {
                    Id = shipments.Count() == 0 ? 1 : shipments.Last().Id + 1,
                    Number = shipments.Count() == 0 ? 1 : shipments.Last().Number + 1,
                    Date = DateTime.Now.Date, //TODO: Datetime provider
                    ShipmentItems = new List<ShipmentItem>()
                },
                Clients = clients.Select(c => new SelectListItem
                {
                    Text = c.Title,
                    Value = c.Id.ToString()
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
                })
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Shipment")] CreateShipmentViewModel shipmentModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var shipments = _memoryCache.Get<List<Shipment>>("Shipments") ?? new List<Shipment>();
                    var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
                    var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
                    var clients = _memoryCache.Get<List<Client>>("Clients")?.Where(u => u.IsActive) ?? new List<Client>();
                    var shipmentItems = _memoryCache.Get<List<ShipmentItem>>("ShipmentItems") ?? new List<ShipmentItem>();
                    var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

                    shipmentModel.Clients = clients.Select(c => new SelectListItem
                    {
                        Text = c.Title,
                        Value = c.Id.ToString()
                    });

                    shipmentModel.Resources = resources.Select(r => new SelectListItem
                    {
                        Text = r.Title,
                        Value = r.Id.ToString()
                    });

                    shipmentModel.Units = units.Select(r => new SelectListItem
                    {
                        Text = r.Title,
                        Value = r.Id.ToString()
                    });

                    if (shipmentModel.Shipment.ShipmentItems != null)
                    {
                        if (shipmentModel.Shipment.IsSigned)
                        {
                            var balanceChecks = shipmentModel.Shipment.ShipmentItems
                                .Select(si => new
                                {
                                    ModelShipmentItem = si,
                                    Balance = balances.SingleOrDefault(b => b.ResourceId == si.ResourceId && b.UnitId == si.UnitId),
                                    BalanceInfo = $"ResourceId: {si.ResourceId}, UnitId: {si.UnitId}"
                                });

                            var insufficientBalance = balanceChecks.FirstOrDefault(bc => 
                                bc.Balance is null || bc.Balance.Quantity < bc.ModelShipmentItem.Quantity);

                            if (insufficientBalance != null)
                            {
                                TempData["Error"] = $"Невозможно подписать отгрузку: недостаточный баланс. {insufficientBalance.BalanceInfo}";

                                foreach (var item in shipmentModel.Shipment.ShipmentItems)
                                {
                                    item.Resource = resources.FirstOrDefault(r => r.Id == item.ResourceId);
                                    item.Unit = units.FirstOrDefault(u => u.Id == item.UnitId);
                                }

                                return View(shipmentModel); //TODO: Add awailable balance to view for each row on load?
                            }
                        }

                        foreach (var item in shipmentModel.Shipment.ShipmentItems) //!
                        {
                            var shipmentItem = new ShipmentItem()
                            {
                                Id = shipmentItems.Count() == 0 ? 1 : shipmentItems.Last().Id + 1,
                                ShipmentId = shipmentModel.Shipment.Id,
                                ResourceId = item.ResourceId,
                                UnitId = item.UnitId,
                                Quantity = item.Quantity
                            };

                            item.Id = shipmentItem.Id;
                            item.Resource = resources.FirstOrDefault(r => r.Id == item.ResourceId);
                            item.Unit = units.FirstOrDefault(u => u.Id == item.UnitId);

                            shipmentItems.Add(shipmentItem);

                            if (shipmentModel.Shipment.IsSigned)
                            {
                                var balance = balances.SingleOrDefault(x => x.ResourceId == item.ResourceId && x.UnitId == item.UnitId);

                                if (balance != null)
                                {
                                    balance.Quantity -= item.Quantity;
                                    if (balance.Quantity == 0) balances.Remove(balance);
                                }
                                else
                                {
                                    TempData["Error"] = "Что-то пошло не так.";
                                    return RedirectToAction(nameof(Index));
                                    //Hanlde error
                                }
                            }
                        }
                        _memoryCache.Set("Balances", balances); //
                        _memoryCache.Set("ShipmentItems", shipmentItems); //
                    }
                    else
                    {
                        shipmentModel.Shipment.ShipmentItems = new List<ShipmentItem>();
                        //TODO: Restrict to create empty shipment
                    }

                    var shipment = new Shipment()
                    {
                        Id = shipmentModel.Shipment.Id,
                        Number = shipmentModel.Shipment.Number,
                        Date = shipmentModel.Shipment.Date,
                        ClientId = shipmentModel.Shipment.ClientId,
                        IsSigned = shipmentModel.Shipment.IsSigned
                    };

                    shipments.Add(shipment);

                    _memoryCache.Set("Shipments", shipments); //

                    TempData["Success"] = "Новая отгрузка успешно добавлена.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = "Обнаружены дублирующиеся балансы. Обратитесь к администратору.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                var shipments = _memoryCache.Get<List<Shipment>>("Shipments") ?? new List<Shipment>();

                var shipment = shipments.SingleOrDefault(x => x.Id == id);

                if (shipment is null)
                {
                    return NotFound();
                }

                if (shipment.IsSigned)
                {
                    return RedirectToAction(nameof(EditSigned), shipment);
                }

                var clients = _memoryCache.Get<List<Client>>("Clients")?.Where(r => r.IsActive) ?? new List<Client>();
                var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
                var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
                var shipmentItemsCache = _memoryCache.Get<List<ShipmentItem>>("ShipmentItems") ?? new List<ShipmentItem>();

                var shipmentItems = JsonSerializer.Deserialize<List<ShipmentItem>>(JsonSerializer.Serialize(shipmentItemsCache));

                var shipmentModel = new Shipment()
                {
                    Id = shipment.Id,
                    Number = shipment.Number,
                    ClientId = shipment.ClientId,
                    Date = shipment.Date,
                    ShipmentItems = shipmentItems.Where(x => x.ShipmentId == shipment.Id).ToList() ?? new List<ShipmentItem>()
                };

                if (shipmentModel.ShipmentItems.Any())
                {
                    foreach (var shipmentItem in shipmentModel.ShipmentItems)
                    {
                        shipmentItem.Resource = resources.FirstOrDefault(r => r.Id == shipmentItem.ResourceId);
                        shipmentItem.Unit = units.FirstOrDefault(u => u.Id == shipmentItem.UnitId);
                    }
                }

                var vm = new EditShipmentViewModel()
                {
                    Shipment = shipmentModel,
                    Clients = clients.Select(c => new SelectListItem
                    {
                        Text = c.Title,
                        Value = c.Id.ToString()
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
                    })
                };

                return View(vm);

            }
            catch (Exception)
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> EditSigned(Shipment shipment)
        {
            if (shipment is null)
            {
                return NotFound();
            }

            try
            {
                var client = _memoryCache.Get<List<Client>>("Clients")?
                    .Where(r => r.IsActive)
                    .FirstOrDefault(x => x.Id == shipment.ClientId) ?? throw new Exception(); //TODO: Handle null client ex

                var shipmentItemsCache = _memoryCache.Get<List<ShipmentItem>>("ShipmentItems")?
                    .Where(x => x.ShipmentId == shipment.Id)
                    .ToList() ?? new List<ShipmentItem>();

                var shipmentItems = JsonSerializer.Deserialize<List<ShipmentItem>>(JsonSerializer.Serialize(shipmentItemsCache)) ?? new List<ShipmentItem>();

                var resources = _memoryCache.Get<List<Resource>>("Resources")?
                    .Where(r => r.IsActive && shipmentItems.Any(si => si.ResourceId == r.Id))
                    .ToList() ?? new List<Resource>();

                var units = _memoryCache.Get<List<Unit>>("Units")?
                    .Where(u => u.IsActive && shipmentItems.Any(si => si.UnitId == u.Id))
                    .ToList() ?? new List<Unit>();

                var shipmentModel = new Shipment()
                {
                    Id = shipment.Id,
                    Number = shipment.Number,
                    ClientId = shipment.ClientId,
                    Client = client,
                    Date = shipment.Date,
                    ShipmentItems = shipmentItems
                };

                if (shipmentModel.ShipmentItems.Any())
                {
                    foreach (var shipmentItem in shipmentModel.ShipmentItems)
                    {
                        shipmentItem.Resource = resources.FirstOrDefault(r => r.Id == shipmentItem.ResourceId);
                        shipmentItem.Unit = units.FirstOrDefault(u => u.Id == shipmentItem.UnitId);
                    }
                }

                var vm = new EditShipmentViewModel()
                {
                    Shipment = shipmentModel
                };

                return View(vm);
            }
            catch (Exception)
            {
                TempData["Error"] = "Что-то пошло не так.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
