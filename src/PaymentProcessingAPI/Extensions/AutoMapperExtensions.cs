using FluentValidation;
using PaymentProcessingAPI.Configurations;

namespace PaymentProcessingAPI.Extensions;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var configuration = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            return configuration.CreateMapper();
        });

        return services;
    }

    public static IServiceCollection AddFluentValidationConfiguration(this IServiceCollection services)
    {
        // FluentValidation configuration removed as no validators are present in the project
        return services;
    }
}