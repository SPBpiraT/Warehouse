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

        public IActionResult Resource()
        {

            return View(new ResourceViewModel { Title = "Banana"});
        }

        public IActionResult Resources()
        {
            return View(new ResourcesListViewModel
            {
                ResourcesList = new List<ResourceViewModel>()
                {
                    new ResourceViewModel { Title = "Peanut" },
                    new ResourceViewModel { Title = "Cherry" }
                }
            });
        }
    }
}
