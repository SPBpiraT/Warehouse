using Microsoft.AspNetCore.Mvc;
using Warehouse.Domain.ViewModels.Resource;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;

namespace Warehouse.Web.Controllers
{
    public class ResourceController : Controller
    {
        private readonly ILogger<ResourceController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ResourceController(ILogger<ResourceController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
            var vm = new ResourcesListViewModel() { Resources = resources.Where(x => x.IsActive).ToList() };
            return View(vm);


        }

        public async Task<IActionResult> Archive()
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
            var vm = new ResourcesListViewModel() { Resources = resources.Where(x => !x.IsActive).ToList() };
            return View(vm);
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title")] Resource resource)
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();

            try
            {
                if (ModelState.IsValid)
                {
                    resources.Add(new Resource()
                    {
                        Id = resources.Count() + 1,
                        Title = resource.Title,
                        IsActive = true
                    });
                    _memoryCache.Set("Resources", resources);
                    TempData["Success"] = "Новый ресурс успешно добавлен.";
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
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
            var vm = resources.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title")] Resource resource)
        {
            if (id != resource.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
                    var res = resources.FirstOrDefault(x => x.Id == id);
                    if (res is not null)
                    {
                        res.Title = resource.Title;
                    }
                    _memoryCache.Set("Resources", resources);
                    TempData["Success"] = "Ресурс успешно отредактирован.";

                }
                catch (Exception)
                {
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(resource);
        }

        public async Task<IActionResult> EditArchived(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
            var vm = resources.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();

            try
            {
                var res = resources.FirstOrDefault(x => x.Id == id);
                if (res is not null)
                {
                    res.IsActive = !res.IsActive;
                }
                _memoryCache.Set("Resources", resources);
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
            var resources = _memoryCache.Get<List<Resource>>("Resources") ?? new List<Resource>();
            try
            {
                var res = resources.FirstOrDefault(x => x.Id == id);
                if (res is not null) resources.Remove(res);

                _memoryCache.Set("Resources", resources);
                TempData["Success"] = "Ресурс успешно удален.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
