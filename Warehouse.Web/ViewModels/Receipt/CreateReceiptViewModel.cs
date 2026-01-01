using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Warehouse.Web.ViewModels.Receipt
{
    public record CreateReceiptViewModel
    {
        public Domain.Entities.Receipt Receipt { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Resources { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> Units { get; set; }
    }
}
