using FluentValidation;
using MediatR;
using ExulofraApi.Common.Models;

namespace ExulofraApi.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            if (
                typeof(TResponse).IsGenericType
                && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>)
            )
            {
                var validationError = new Error(
                    ErrorType.Validation,
                    "Validasyon hatası oluştu.",
                    failures
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray())
                );

                return (TResponse)
                    typeof(Result<>)
                        .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                        .GetMethod("Failure")!
                        .Invoke(null, [validationError])!;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
