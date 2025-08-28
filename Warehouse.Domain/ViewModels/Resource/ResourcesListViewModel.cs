namespace Warehouse.Domain.ViewModels.Resource
{
    public record ResourcesListViewModel
    {
        public IEnumerable<Entities.Resource>? Resources { get; set; }
    }
}
