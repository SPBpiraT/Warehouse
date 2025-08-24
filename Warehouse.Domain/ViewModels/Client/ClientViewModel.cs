using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.ViewModels.Client
{
    public record ClientViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Наименование")]
        public string Title { get; set; }

        [Display(Name = "Адрес")]
        public string Address { get; set; }

        public bool IsActive { get; set; }
    }
}
