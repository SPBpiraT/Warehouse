using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.Entities
{
    public record Client
    {
        public int Id { get; set; }

        [Display(Name = "Клиент")]
        public string Title { get; set; }

        [Display(Name = "Адрес")]
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}
