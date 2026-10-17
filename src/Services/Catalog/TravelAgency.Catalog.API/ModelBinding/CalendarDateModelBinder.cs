using Microsoft.AspNetCore.Mvc.ModelBinding;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

namespace TravelAgency.Catalog.API.ModelBinding;

/// <summary>
/// Принимает в query и дату без зоны (<c>2026-11-01</c>), и прежний вид с временем
/// (<c>2026-11-01T00:00:00Z</c>). В модель попадает календарный день, как он написан.
/// </summary>
internal sealed class CalendarDateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var type = context.Metadata.ModelType;
        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            return new CalendarDateModelBinder();
        return null;
    }
}

internal sealed class CalendarDateModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        var text = value.FirstValue;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) is not null)
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DepartureCalendarDate.TryParse(text, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "Date must be a calendar day (yyyy-MM-dd).");
        return Task.CompletedTask;
    }
}
