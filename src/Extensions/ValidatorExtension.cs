using System.Reflection;
using FluentValidation;

namespace ContractWatcher.Core.Extensions;

public static class ValidatorExtension
{
    public static IServiceCollection AddValidators(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies is null || assemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }
}