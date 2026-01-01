namespace Warehouse.Web.ViewModels.Resource
{
    public record ResourcesListViewModel
    {
        public IEnumerable<Domain.Entities.Resource>? Resources { get; set; }
    }
}
