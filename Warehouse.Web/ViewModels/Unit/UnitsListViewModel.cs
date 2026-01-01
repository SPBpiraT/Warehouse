namespace Warehouse.Web.ViewModels.Unit
{
    public record UnitsListViewModel
    {
        public IEnumerable<Domain.Entities.Unit> Units { get; set; }
    }
}
