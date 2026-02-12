using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace RdtClient.Service.Helpers;

public class JsonModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult != ValueProviderResult.None)
        {
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var valueAsString = valueProviderResult.FirstValue ?? "";
            var result = JsonConvert.DeserializeObject(valueAsString, bindingContext.ModelType);

            if (result != null)
            {
                bindingContext.Result = ModelBindingResult.Success(result);

                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
