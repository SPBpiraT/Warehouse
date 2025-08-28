namespace Warehouse.Domain.ViewModels.Unit
{
    public record UnitsListViewModel
    {
        public IEnumerable<Entities.Unit> Units { get; set; }
    }
}
