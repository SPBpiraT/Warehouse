using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.ViewModels.Shipment
{
    public record ShipmentsListViewModel
    {
        public IEnumerable<Entities.Shipment> Shipments { get; set; }

        [Display(Name = "Номер отгрузки")]
        public IEnumerable<SelectListItem> ShipmentsNumbers { get; set; }

        [Display(Name = "Ресуры")]
        public IEnumerable<SelectListItem> Resources { get; set; }

        [Display(Name = "Единицы измерения")]
        public IEnumerable<SelectListItem> Units { get; set; }

        [Display(Name = "Клиенты")]
        public IEnumerable<SelectListItem> Clients { get; set; }

        public string DateRange { get; set; }
        public IEnumerable<int> SelectedNumbers { get; set; }
        public IEnumerable<int> SelectedResources { get; set; }
        public IEnumerable<int> SelectedUnits { get; set; }
        public IEnumerable<int> SelectedClients { get; set; }
    }
}
