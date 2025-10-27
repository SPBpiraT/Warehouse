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
            var shipments = _memoryCache.Get<List<Shipment>>("Shipments") ?? new List<Shipment>();
            var resources = _memoryCache.Get<List<Resource>>("Resources")?.Where(r => r.IsActive) ?? new List<Resource>();
            var units = _memoryCache.Get<List<Unit>>("Units")?.Where(u => u.IsActive) ?? new List<Unit>();
            var shipmentItems = _memoryCache.Get<List<ShipmentItem>>("ShipmentItems") ?? new List<ShipmentItem>();
            var balances = _memoryCache.Get<List<Balance>>("Balances") ?? new List<Balance>();

            try
            {
                if (ModelState.IsValid)
                {
                    //TODO: add check for existing entity id if(!_repo.Exist(id))

                    if (shipmentModel.Shipment.ShipmentItems != null)
                    {
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

                            if (shipmentModel.Shipment.IsSigned == true)
                            {
                                var balanceCached = balances.SingleOrDefault(x => x.ResourceId == item.ResourceId && x.UnitId == item.UnitId);

                                if (balanceCached != null)
                                {
                                    balanceCached.Quantity -= item.Quantity;
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
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка!");
            }

            //
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

            return View(shipmentModel);
        }
    }
}
