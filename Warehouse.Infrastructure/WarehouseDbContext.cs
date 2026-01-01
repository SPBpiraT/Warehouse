using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Interfaces;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure
{
    public class WarehouseDbContext : DbContext, IWarehouseDbContext
    {
        public DbSet<Balance> Balances { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptItem> ReceiptItems { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }
        public DbSet<Unit> Units { get; set; }

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
            : base(options) 
        {
            Database.EnsureCreated();
        }
    }
}
