using Microsoft.AspNetCore.Mvc.Rendering;

namespace Warehouse.Domain.ViewModels.Balance
{
    public record BalancesListViewModel
    {
       public IEnumerable<Entities.Balance> Balances { get; set; }
       public SelectList Resources { get; set; } = new SelectList(new List<Entities.Resource>(), "Id", "Title");
       public SelectList Units { get; set; } = new SelectList(new List<Entities.Unit>(), "Id", "Title");
    }
}
