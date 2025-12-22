using Microsoft.Extensions.Caching.Memory;
using Warehouse.Domain.Entities;

namespace Warehouse.Web.Helpers
{
    public class BalanceHelper
    {
        public static void UpdateBalanceFromReceipt(
            IMemoryCache context, //context
            List<ReceiptItem> previousItems,
            List<ReceiptItem> newItems)
        {
            UpdateBalance(context, newItems);
            UpdateBalance(context, previousItems, true);
        }

        private static void UpdateBalance(
            IMemoryCache context, //context
             List<ReceiptItem> items, 
            bool isSubstraction = false)
        {
            var balances = context.Get<List<Balance>>("Balances") ?? new List<Balance>();
            var balancesToUpdate = new List<Balance>();

            foreach (var item in items)
            {
                var balance = balances.SingleOrDefault(b => b.ResourceId == item.ResourceId && b.UnitId == item.UnitId);

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
                    Id = balances.Count() == 0 ? 1 : balances.Last().Id + 1, //
                    Quantity = r.Quantity,
                    ResourceId = r.ResourceId,
                    UnitId = r.UnitId
                }).ToList();

                balances.AddRange(missingBalances);
            }
        }

        public static async Task<bool> IsInvalidReceiptUpdate(
            IMemoryCache context, //context
            List<ReceiptItem> previousItems,
            List<ReceiptItem> newItems)
        {
            var balances = context.Get<List<Balance>>("Balances") ?? new List<Balance>();

            foreach (var previousItem in previousItems)
            {
                var newItem = newItems.FirstOrDefault(r => r.Id == previousItem.Id && (r.ResourceId == previousItem.ResourceId && r.UnitId == previousItem.UnitId));

                var newItemQuantity = newItem?.Quantity ?? 0;

                if (newItemQuantity > previousItem.Quantity)
                {
                    continue;
                }

                var quantityDelta = previousItem.Quantity - newItemQuantity;

                var hasInvalidBalance = balances //await context
                    .Where(r => r.ResourceId == previousItem.ResourceId && r.UnitId == previousItem.UnitId)
                    .Where(r => r.Quantity < quantityDelta)
                    .Any();

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
