using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure;
using Warehouse.Web.ViewModels.Unit;

namespace Warehouse.Web.Controllers
{
    public class UnitController : Controller
    {
        private readonly ILogger<UnitController> _logger;
        //private readonly IMemoryCache _memoryCache;
        private readonly WarehouseDbContext _warehouseDbContext;

        public UnitController(ILogger<UnitController> logger, 
            IMemoryCache memoryCache, 
            WarehouseDbContext warehouseDbContext)
        {
            _logger = logger;
            //_memoryCache = memoryCache;
            _warehouseDbContext = warehouseDbContext;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var units = await _warehouseDbContext.Units.Where(x => x.IsActive).ToListAsync();
            var vm = new UnitsListViewModel() { Units = units };
            return View(vm);
        }

        public async Task<IActionResult> Archive()
        {
            var units = await _warehouseDbContext.Units.Where(x => !x.IsActive).ToListAsync();
            var vm = new UnitsListViewModel() { Units = units };
            return View(vm);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title")] Unit unit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _warehouseDbContext.Units
                            .FirstOrDefaultAsync(u => u.Title == unit.Title);

                    if (entity != null)
                    {
                        TempData["Error"] = "Уже существует единица с таким же наименованием.";
                        return RedirectToAction(nameof(Create));
                    }

                    unit.IsActive = true;

                    await _warehouseDbContext.Units.AddAsync(unit);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Новая единица измерения успешно добавлена.";
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
            var entity = await _warehouseDbContext.Units.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,IsActive")] Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _warehouseDbContext.Units.AnyAsync(x => x.Id == unit.Id);

                    if (!exists)
                    {
                        TempData["Error"] = "Единица измерения не найдена.";
                        return RedirectToAction(nameof(Index));
                    }

                    _warehouseDbContext.Units.Update(unit);
                    await _warehouseDbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                TempData["Success"] = "Единица измерения успешно отредактирована.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Ошибка валидации.";
            return View(unit);
        }

        public async Task<IActionResult> EditArchived(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var entity = await _warehouseDbContext.Units.FirstOrDefaultAsync(x => x.Id == id);

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
                var entity = await _warehouseDbContext.Units.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Единица измерения не найдена.";
                }
                else
                {
                    entity.IsActive = !entity.IsActive;

                    _warehouseDbContext.Units.Update(entity);
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
                var entity = await _warehouseDbContext.Units.FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                {
                    TempData["Error"] = "Единица измерения не найдена.";
                }
                else
                {
                    _warehouseDbContext.Units.Remove(entity);
                    await _warehouseDbContext.SaveChangesAsync();

                    TempData["Success"] = "Единица измерения успешно удалена.";
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
