namespace Warehouse.Domain.ViewModels.Client
{
    public record ClientsListViewModel
    {
        public IEnumerable<Entities.Client> Clients { get; set; }
    }
}
