using Microsoft.AspNetCore.Mvc.Rendering;

namespace Warehouse.Domain.ViewModels.Balance
{
    public record BalancesListViewModel
    {
       public List<Entities.Balance> BalancesList { get; set; } //IEnumerable
       public SelectList Resources { get; set; } = new SelectList(new List<Entities.Resource>(), "Id", "Title");
       public SelectList Units { get; set; } = new SelectList(new List<Entities.Unit>(), "Id", "Title");
    }
}
