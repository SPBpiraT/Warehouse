namespace Warehouse.Domain.ViewModels.Client
{
    public record ClientsListViewModel
    {
        public List<ClientViewModel>? ClientsList { get; set; }
    }
}
