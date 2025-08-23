using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.ViewModels.Resource
{
    public record ResourceViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Наименование")]
        public string Title { get; set; }

        public bool IsActive { get; set; }
    }
}
