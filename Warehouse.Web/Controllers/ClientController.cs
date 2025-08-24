using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.ViewModels.Client;

namespace Warehouse.Web.Controllers
{
    public class ClientController : Controller
    {
        private readonly ILogger<ClientController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ClientController(ILogger<ClientController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
            //UoW GetAll
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
            var vm = new ClientsListViewModel() { ClientsList = clients.ClientsList.Where(x => x.IsActive).ToList() };
            return View(vm);


        }

        public async Task<IActionResult> Archive()
        {
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
            var vm = new ClientsListViewModel() { ClientsList = clients.ClientsList.Where(x => !x.IsActive).ToList() };
            return View(vm);
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Title,Address")] ClientViewModel client)
        {
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };

            try
            {
                if (ModelState.IsValid)
                {
                    clients.ClientsList.Add(new ClientViewModel()
                    {
                        Id = clients.ClientsList.Count() + 1,
                        Title = client.Title,
                        Address = client.Address,
                        IsActive = true
                    });
                    _memoryCache.Set("Clients", clients);
                    TempData["Success"] = "Новый клиент успешно добавлен.";
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
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
            var vm = clients.ClientsList.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Address")] ClientViewModel client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
                    var cli = clients.ClientsList.FirstOrDefault(x => x.Id == id);
                    if (cli is not null)
                    {
                        cli.Title = client.Title;
                        cli.Address = client.Address;
                    }
                    _memoryCache.Set("Clients", clients);
                    TempData["Success"] = "Клиент успешно отредактирован.";

                }
                catch (Exception)
                {
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> EditArchived(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //UoW Get
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
            var vm = clients.ClientsList.FirstOrDefault(x => x.Id == id);

            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };

            try
            {
                var cli = clients.ClientsList.FirstOrDefault(x => x.Id == id);
                if (cli is not null)
                {
                    cli.IsActive = !cli.IsActive;
                }
                _memoryCache.Set("Clients", clients);
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
            var clients = _memoryCache.Get<ClientsListViewModel>("Clients") ?? new ClientsListViewModel() { ClientsList = new List<ClientViewModel>() };
            try
            {
                var cli = clients.ClientsList.FirstOrDefault(x => x.Id == id);
                if (cli is not null) clients.ClientsList.Remove(cli);
                TempData["Success"] = "Клиент успешно удален.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
