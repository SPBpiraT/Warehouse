namespace Warehouse.Web.ViewModels.Client
{
    public record ClientsListViewModel
    {
        public IEnumerable<Domain.Entities.Client> Clients { get; set; }
    }
}
