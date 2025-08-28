using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.Entities
{
    public record Unit
    {
        public int Id { get; set; }

        [Display(Name = "Наименование")]
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
}
