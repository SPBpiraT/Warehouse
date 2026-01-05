using Microsoft.AspNetCore.Mvc;
using Warehouse.Web.ViewModels.Resource;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Warehouse.Web.Controllers
{
    public class ResourceController : Controller
    {
        private readonly ILogger<ResourceController> _logger;
        private readonly WarehouseDbContext _warehouseDbContext;

        public ResourceController(ILogger<ResourceController> logger, 
            WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            _warehouseDbContext = warehouseDbContext;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var resources = await _warehouseDbContext.Resources.Where(x => x.IsActive).ToListAsync();
            var vm = new ResourcesListViewModel() { Resources = resources };
            return View(vm);
        }

        public async Task<IActionResult> Archive()
        {
            var resources = await _warehouseDbContext.Resources.Where(x => !x.IsActive).ToListAsync();
            var vm = new ResourcesListViewModel() { Resources = resources };
            return View(vm);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title")] Resource resource)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _warehouseDbContext.Resources
                            .FirstOrDefaultAsync(r => r.Title == resource.Title);

                    if (entity != null)
                    {
                        TempData["Error"] = "Уже существует ресурс с таким же наименованием.";
                        return RedirectToAction(nameof(Create));
                    }

                    resource.IsActive = true;

                    await _warehouseDbContext.Resources.AddAsync(resource);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Новый ресурс успешно добавлен.";
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
            var entity = await _warehouseDbContext.Resources.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,IsActive")] Resource resource)
        {
            if (id != resource.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _warehouseDbContext.Resources.AnyAsync(x => x.Id == resource.Id);

                    if (!exists)
                    {
                        TempData["Error"] = "Ресурс не найден.";
                        return RedirectToAction(nameof(Index));
                    }

                    _warehouseDbContext.Resources.Update(resource);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Ресурс успешно отредактирован.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Ошибка валидации.";
            return View(resource);
        }

        public async Task<IActionResult> EditArchived(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var entity = await _warehouseDbContext.Resources.FirstOrDefaultAsync(x => x.Id == id);

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
                var entity = await _warehouseDbContext.Resources.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Ресурс не найден.";
                }
                else
                {
                    entity.IsActive = !entity.IsActive;

                    _warehouseDbContext.Resources.Update(entity);
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
                var entity = await _warehouseDbContext.Resources.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Ресурс не найден.";
                }
                else
                {
                    _warehouseDbContext.Resources.Remove(entity);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Ресурс успешно удален.";
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
