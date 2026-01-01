using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Warehouse.Web.ViewModels.Shipment
{
    public record EditShipmentViewModel
    {
        public Domain.Entities.Shipment Shipment { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Clients { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Resources { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Units { get; set; }
    }
}
