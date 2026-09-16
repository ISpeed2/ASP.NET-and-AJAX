using FluentValidation;

namespace MyApi;

public class TaskRequestValidator : AbstractValidator<TaskRequest>
{
    public TaskRequestValidator()
    {
        RuleFor(task => task.Title)
            .NotEmpty().WithMessage("Название задачи обязательно")
            .MaximumLength(200).WithMessage("Название не может быть длиннее 200 символов");

        RuleFor(task => task.Description)
            .MaximumLength(2000).WithMessage("Описание не может быть длиннее 2000 символов");
    }
}