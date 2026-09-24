using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Shop.Api.Binding;

public sealed class CsvGuidArrayBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var raw = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var values = new List<Guid>();
        foreach (var value in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(value, out var id))
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{value}' is not a valid GUID");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            values.Add(id);
        }

        bindingContext.Result = ModelBindingResult.Success(values.ToArray());
        return Task.CompletedTask;
    }
}