namespace Warehouse.Web.Models.Resource
{
    public record ResourcesListViewModel
    {
        public IEnumerable<ResourceViewModel>? ResourcesList { get; set; }
    }
}
