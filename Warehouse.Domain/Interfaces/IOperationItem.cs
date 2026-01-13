using Warehouse.Domain.Entities;

namespace Warehouse.Domain.Interfaces
{
    public interface IOperationItem
    {
        int Id { get; set; }
        ulong Quantity { get; set; }
        int ResourceId { get; set; }
        Resource Resource { get; set; }
        int UnitId { get; set; }
        Unit Unit { get; set; }
    }
}
