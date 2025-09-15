using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.ViewModels.Receipt
{
    public record ReceiptsListViewModel
    {
        public IEnumerable<Entities.Receipt> Receipts { get; set; }

        [Display(Name = "Номер поступления")]
        public IEnumerable<SelectListItem> ReceiptNumbers{ get; set; }

        [Display(Name = "Ресуры")]
        public IEnumerable<SelectListItem> Resources { get; set; }

        [Display(Name = "Единицы измерения")]
        public IEnumerable<SelectListItem> Units { get; set; }

        public string DateRange { get; set; }
        public IEnumerable<int> SelectedNumbers { get; set; }
        public IEnumerable<int> SelectedResources { get; set; }
        public IEnumerable<int> SelectedUnits { get; set; }

    }
}
