using System.ComponentModel.DataAnnotations;

namespace HW3.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название товара.")]
    [StringLength(100, ErrorMessage = "Название не должно быть длиннее 100 символов.")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000000, ErrorMessage = "Цена должна быть больше нуля.")]
    [Display(Name = "Цена")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Укажите категорию.")]
    [StringLength(80, ErrorMessage = "Категория не должна быть длиннее 80 символов.")]
    [Display(Name = "Категория")]
    public string Category { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Описание не должно быть длиннее 500 символов.")]
    [Display(Name = "Описание")]
    public string Description { get; set; } = string.Empty;
}
