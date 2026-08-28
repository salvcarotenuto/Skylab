using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyLab.Web.Infrastructure;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;

        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            if (Nullable.GetUnderlyingType(context.ModelType) is not null)
                context.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var culture = text.Contains(',') ? Italian : CultureInfo.InvariantCulture;
        if (decimal.TryParse(text, NumberStyles.Number, culture, out var parsed))
        {
            context.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        context.ModelState.TryAddModelError(context.ModelName, "Inserire un valore numerico valido.");
        return Task.CompletedTask;
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
