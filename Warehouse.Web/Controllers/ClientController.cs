using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure;
using Warehouse.Web.ViewModels.Client;

namespace Warehouse.Web.Controllers
{
    public class ClientController : Controller
    {
        private readonly ILogger<ClientController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        public ClientController(ILogger<ClientController> logger,
            WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var clients = await _warehouseDbContext.Clients.Where(x => x.IsActive).ToListAsync();
            var vm = new ClientsListViewModel() { Clients = clients };
            return View(vm);
        }

        public async Task<IActionResult> Archive()
        {
            var clients = await _warehouseDbContext.Clients.Where(x => !x.IsActive).ToListAsync();
            var vm = new ClientsListViewModel() { Clients = clients };
            return View(vm);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title,Address")] Client client)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _warehouseDbContext.Clients
                            .FirstOrDefaultAsync(c => c.Title == client.Title);

                    if (entity != null)
                    {
                        TempData["Error"] = "Уже существует клиент с таким же наименованием.";
                        return RedirectToAction(nameof(Create));
                    }

                    client.IsActive = true;

                    await _warehouseDbContext.Clients.AddAsync(client);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Новый клиент успешно добавлен.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Ошибка валидации.";
            return View();
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var entity = await _warehouseDbContext.Clients.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Address,IsActive")] Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _warehouseDbContext.Clients.AnyAsync(x => x.Id == client.Id);

                    if (!exists)
                    {
                        TempData["Error"] = "Клиент не найден.";
                        return RedirectToAction(nameof(Index));
                    }

                    _warehouseDbContext.Clients.Update(client);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Клиент успешно отредактирован.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Ошибка валидации.";
            return View(client);
        }

        public async Task<IActionResult> EditArchived(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var entity = await _warehouseDbContext.Clients.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            try
            {
                var entity = await _warehouseDbContext.Clients.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Клиент не найден.";
                }
                else
                {
                    entity.IsActive = !entity.IsActive;

                    _warehouseDbContext.Clients.Update(entity);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Статус изменен.";
                }
            }
            catch(Exception)
            {
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _warehouseDbContext.Clients.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Клиент не найден.";
                }
                else
                {
                    _warehouseDbContext.Clients.Remove(entity);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Клиент успешно удален.";
                }
            }
            catch(Exception)
            {
                throw;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
