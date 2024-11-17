using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
    
        public ValidationBehavior(IValidator<TRequest>[] validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }
    
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
    
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
    
                if (failures.Count != 0)
                {
                    _logger.LogWarning("Validation errors for type {RequestType}: {Errors}", typeof(TRequest).Name, failures);
                    throw new ValidationException(failures);
                }
            }
    
            return await next();
        }
    }
}