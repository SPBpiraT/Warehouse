using Microsoft.AspNetCore.Mvc;
using Warehouse.Web.Models.Resource;
using Microsoft.Extensions.Caching.Memory;

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
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var vm = new ResourcesListViewModel() { ResourcesList = resources.ResourcesList.Where(x => x.IsActive).ToList() };
            return View(vm);


        }

        public async Task<IActionResult> Archive()
        {
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var vm = new ResourcesListViewModel() { ResourcesList = resources.ResourcesList.Where(x => !x.IsActive).ToList() };
            return View(vm);
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title")] ResourceViewModel resource)
        {
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>()};
            resources.ResourcesList.Add(new ResourceViewModel()
            {
                Id = resources.ResourcesList.Count() + 1,
                Title = resource.Title,
                IsActive = true
            });
            _memoryCache.Set("Resources", resources);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            //UoW Get
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var vm = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title")] ResourceViewModel resource)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
                    var res = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
                    if (res is not null)
                    {
                        res.Title = resource.Title;
                    }
                    _memoryCache.Set("Resources", resources);

                }
                catch (Exception)
                {

                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditArchived(int id)
        {
            //UoW Get
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var vm = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MoveToArchive(int id)
        {
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var res = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
            if (res is not null && res.IsActive)
            {
                res.IsActive = false;
            }
            _memoryCache.Set("Resources", resources);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MoveToActual(int id)
        {
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var res = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
            if (res is not null && !res.IsActive)
            {
                res.IsActive = true;
            }
            _memoryCache.Set("Resources", resources);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var resources = _memoryCache.Get<ResourcesListViewModel>("Resources") ?? new ResourcesListViewModel() { ResourcesList = new List<ResourceViewModel>() };
            var res = resources.ResourcesList.FirstOrDefault(x => x.Id == id);
            if (res is not null) resources.ResourcesList.Remove(res); 
            return RedirectToAction(nameof(Index));
        }
    }
}
