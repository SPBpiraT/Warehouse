using Microsoft.AspNetCore.Mvc;
using Warehouse.Web.Models.Resource;

namespace Warehouse.Web.Controllers
{
    public class ResourceController : Controller
    {
        private readonly ILogger<ResourceController> _logger;

        public ResourceController(ILogger<ResourceController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var resources = new ResourcesListViewModel()
            {
                //UoW Get
                ResourcesList = new List<ResourceViewModel>()
                {
                    new ResourceViewModel
                    {
                        Id = 1,
                        Title = "Peanut",
                        IsActive = true
                    },
                    new ResourceViewModel
                    {
                        Id = 2,
                        Title = "Cherry",
                        IsActive = true
                    }
                }
            };
            return View(resources);
        }

        public async Task<IActionResult> Archive()
        {
            return View();
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Title")] ResourceViewModel resource)
        {
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            //UoW Get
            var resource = new ResourceViewModel
            {
                Id = id,
                Title = "Get"
            };

            return View(resource);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title")] ResourceViewModel resource)
        {

            if (ModelState.IsValid)
            {
                try
                {

                }
                catch (Exception)
                {

                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MoveToArchive(int id)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
