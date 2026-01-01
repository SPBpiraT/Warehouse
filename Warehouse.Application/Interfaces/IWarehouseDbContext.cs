using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Interfaces
{
    public interface IWarehouseDbContext
    {
        DbSet<Balance> Balances { get; set; }
        DbSet<Client> Clients { get; set; }
        DbSet<Receipt> Receipts { get; set; }
        DbSet<ReceiptItem> ReceiptItems { get; set; }
        DbSet<Resource> Resources { get; set; }
        DbSet<Shipment> Shipments { get; set; }
        DbSet<ShipmentItem> ShipmentItems { get; set; }
        DbSet<Unit> Units { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
