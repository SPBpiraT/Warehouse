using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.Entities
{
    public class Shipment
    {
        public int Id { get; set; }
        [Display(Name = "Номер")]
        public int Number { get; set; }
        [Display(Name = "Дата")]
        public DateTime Date { get; set; }
        public int ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; }
        public bool IsSigned { get; set; }
        [ValidateNever]
        public IList<ShipmentItem> ShipmentItems { get; set; }
    }
}
