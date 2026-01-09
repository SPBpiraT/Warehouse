using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure;

namespace Warehouse.Web.Helpers
{
    public class BalanceHelper
    {
        public static void UpdateBalanceFromReceipt(
            WarehouseDbContext context,
            List<ReceiptItem> previousItems,
            List<ReceiptItem> newItems)
        {
            UpdateBalance(context, newItems);
            UpdateBalance(context, previousItems, true);
        }

        private static void UpdateBalance(
            WarehouseDbContext context,
            List<ReceiptItem> items, 
            bool isSubstraction = false)
        {
            var balancesToUpdate = new List<Balance>();

            foreach (var item in items)
            {
                var balance = context.Balances.SingleOrDefault(b => b.ResourceId == item.ResourceId && b.UnitId == item.UnitId);

                if (balance != null)
                {
                    if (isSubstraction)
                    {
                        balance.Quantity -= item.Quantity;
                    }
                    else
                    {
                        balance.Quantity += item.Quantity;
                    }

                    balancesToUpdate.Add(balance);
                }
                //TODO: Throw ex if isSubstraction == true && balance == null
                //TODO: Handle InvalidOperationException
            }

            if (!isSubstraction)
            {
                var missingBalances = items
                .Where(r => balancesToUpdate.All(b => b.ResourceId != r.ResourceId || b.UnitId != r.UnitId))
                .Select(r => new Balance
                {
                    Quantity = r.Quantity,
                    ResourceId = r.ResourceId,
                    UnitId = r.UnitId
                }).ToList();

                balancesToUpdate.AddRange(missingBalances);
                context.Balances.UpdateRange(balancesToUpdate);
            }
        }

        public static async Task<bool> IsInvalidReceiptDeletion(WarehouseDbContext context, int id)
        {
            return await context.Balances
                .Select(b =>
                 context.ReceiptItems
                        .Where(r => r.ReceiptId == id)
                        .Where(r => r.ResourceId == b.ResourceId && r.UnitId == b.UnitId)
                        .FirstOrDefault(r => r.Quantity > b.Quantity)
                )
                .Where(r => r != null)
                .AnyAsync();
        }
        public static async Task<bool> IsInvalidReceiptUpdate(
            WarehouseDbContext context, //context
            List<ReceiptItem> previousItems,
            List<ReceiptItem> newItems)
        {
            foreach (var previousItem in previousItems)
            {
                var newItem = newItems.FirstOrDefault(r => r.Id == previousItem.Id && (r.ResourceId == previousItem.ResourceId && r.UnitId == previousItem.UnitId));
                var newItemQuantity = newItem?.Quantity ?? 0;

                if (newItemQuantity > previousItem.Quantity)
                {
                    continue;
                }

                var quantityDelta = previousItem.Quantity - newItemQuantity;

                var hasInvalidBalance = await context.Balances
                    .Where(r => r.ResourceId == previousItem.ResourceId && r.UnitId == previousItem.UnitId)
                    .Where(r => r.Quantity < quantityDelta)
                    .AnyAsync();

                if (hasInvalidBalance)
                {
                    return true;
                    //TODO: Create a ReceiptUpdateResult class instead of returning a boolean value to indicate insufficient balances
                }
            }

            return false;
        }
    }
}
