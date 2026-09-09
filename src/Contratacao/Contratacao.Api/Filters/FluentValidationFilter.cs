using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Contratacao.Api.Filters;

public sealed class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, argument) in context.ActionArguments)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument)!;

            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!validationResult.IsValid)
            {
                var errorDict = new Dictionary<string, string[]>();
                foreach (var error in validationResult.Errors)
                {
                    if (errorDict.TryGetValue(error.PropertyName, out var existing))
                    {
                        errorDict[error.PropertyName] = [.. existing, error.ErrorMessage];
                    }
                    else
                    {
                        errorDict[error.PropertyName] = [error.ErrorMessage];
                    }
                }

                context.Result = new BadRequestObjectResult(new { errors = errorDict });
                return;
            }
        }

        await next();
    }
}
