using System.ComponentModel.DataAnnotations;

namespace ASP3.Models;

public class QuestionnaireRequest
{
    [Required(ErrorMessage = "Укажите имя.")]
    [StringLength(80, ErrorMessage = "Имя не должно быть длиннее 80 символов.")]
    [Display(Name = "Имя")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите фамилию.")]
    [StringLength(80, ErrorMessage = "Фамилия не должна быть длиннее 80 символов.")]
    [Display(Name = "Фамилия")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите электронную почту.")]
    [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите возраст.")]
    [Range(14, 120, ErrorMessage = "Возраст должен быть от 14 до 120 лет.")]
    [Display(Name = "Возраст")]
    public int? Age { get; set; }

    [Required(ErrorMessage = "Укажите город.")]
    [StringLength(100, ErrorMessage = "Название города не должно быть длиннее 100 символов.")]
    [Display(Name = "Город")]
    public string City { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "О себе можно написать не более 500 символов.")]
    [Display(Name = "О себе")]
    public string? About { get; set; }
}