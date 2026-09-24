using FluentValidation;

namespace Shop.Api.Validation;

public sealed record CreateProductRequest(string Name, decimal Price, int Stock);

public sealed record UpdateProductRequest(string Name, decimal Price, int Stock);

public sealed record PatchProductRequest(string? Name, decimal? Price, int? Stock);

public sealed record ProductResponse(Guid Id, string Name, decimal Price, int Stock);

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        When(x => x.Price > 10_000, () =>
            RuleFor(x => x.Name).MinimumLength(10)
                .WithMessage("Дорогие товары требуют длинного названия"));
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}