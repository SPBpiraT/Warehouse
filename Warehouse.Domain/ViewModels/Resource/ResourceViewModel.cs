namespace Warehouse.Domain.ViewModels.Resource
{
    public record ResourceViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
}
