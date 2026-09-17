using System.ComponentModel.DataAnnotations;

namespace MiddlewareHomework.Models;

public sealed class RegistrationRequest
{
    [Required(ErrorMessage = "Введите имя.")]
    [StringLength(60, ErrorMessage = "Имя должно быть не длиннее 60 символов.")]
    [Display(Name = "Имя")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите электронную почту.")]
    [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите направление.")]
    [Display(Name = "Направление")]
    public string Direction { get; set; } = string.Empty;
}