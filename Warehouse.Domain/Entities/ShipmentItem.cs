using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Warehouse.Domain.Interfaces;

namespace Warehouse.Domain.Entities
{
    public class ShipmentItem : IOperationItem
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        [ValidateNever]
        public Resource Resource { get; set; }
        [ValidateNever]
        public Unit Unit { get; set; }
        public ulong Quantity { get; set; }
    }
}
