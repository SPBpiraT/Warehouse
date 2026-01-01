using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Web.ViewModels.Balance
{
    public record BalancesListViewModel
    {
        public IEnumerable<Domain.Entities.Balance> Balances { get; set; }

        [Display(Name = "Ресуры")]
        public IEnumerable<SelectListItem> Resources { get; set; }

        [Display(Name = "Единицы измерения")]
        public IEnumerable<SelectListItem> Units { get; set; }
        public IEnumerable<int> SelectedResources { get; set; }
        public IEnumerable<int> SelectedUnits { get; set; }
    }
}
