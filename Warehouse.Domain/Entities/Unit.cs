namespace Warehouse.Domain.Entities
{
    public record Unit
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
}
