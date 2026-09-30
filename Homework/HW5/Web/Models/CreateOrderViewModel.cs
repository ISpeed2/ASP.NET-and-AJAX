using System.ComponentModel.DataAnnotations;

namespace ASP4.Web.Models;

public sealed class CreateOrderViewModel
{
    [Required(ErrorMessage = "Укажите email клиента")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Добавьте хотя бы один товар")]
    public List<CreateOrderItemViewModel> Items { get; set; } = [new()];
}

public sealed class CreateOrderItemViewModel
{
    [Required(ErrorMessage = "Укажите ProductId")]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше нуля")]
    public int Quantity { get; set; } = 1;
}