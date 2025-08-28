using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;
using Warehouse.Domain.ViewModels.Unit;

namespace Warehouse.Web.Controllers
{
    public class UnitController : Controller
    {
        private readonly ILogger<UnitController> _logger;
        private readonly IMemoryCache _memoryCache;

        public UnitController(ILogger<UnitController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
            var vm = new UnitsListViewModel() { Units = units.Where(x => x.IsActive).ToList() };
            return View(vm);


        }

        public async Task<IActionResult> Archive()
        {
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
            var vm = new UnitsListViewModel() { Units = units.Where(x => !x.IsActive).ToList() };
            return View(vm);
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title")] Unit unit)
        {
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();

            try
            {
                if (ModelState.IsValid)
                {
                    units.Add(new Unit()
                    {
                        Id = units.Count() + 1,
                        Title = unit.Title,
                        IsActive = true
                    });
                    _memoryCache.Set("Units", units);
                    TempData["Success"] = "Новая единица измерения успешно добавлена.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка!");
            }

            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
            var vm = units.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title")] Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
                    var un = units.FirstOrDefault(x => x.Id == id);
                    if (un is not null)
                    {
                        un.Title = unit.Title;
                    }
                    _memoryCache.Set("Units", units);
                    TempData["Success"] = "Единица измерения успешно отредактирована.";

                }
                catch (Exception)
                {
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(unit);
        }

        public async Task<IActionResult> EditArchived(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
            var vm = units.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();

            try
            {
                var unit = units.FirstOrDefault(x => x.Id == id);
                if (unit is not null)
                {
                    unit.IsActive = !unit.IsActive;
                }

                _memoryCache.Set("Units", units);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var units = _memoryCache.Get<List<Unit>>("Units") ?? new List<Unit>();
            try
            {
                var unit = units.FirstOrDefault(x => x.Id == id);
                if (unit is not null) units.Remove(unit);

                _memoryCache.Set("Units", units);
                TempData["Success"] = "Единица измерения успешно удалена.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
