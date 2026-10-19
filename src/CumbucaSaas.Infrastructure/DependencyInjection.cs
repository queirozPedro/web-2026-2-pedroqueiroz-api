using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CumbucaSaas.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CumbucaDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' não configurada.");

        services.AddDbContext<CumbucaDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();

        return services;
    }
}
