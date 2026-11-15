using CumbucaSaas.Domain.Aggregates.CalendarioAggregate;
using CumbucaSaas.Domain.Aggregates.ConsumoAggregate;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
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
        services.AddScoped<ICalendarioRefeicaoRepository, CalendarioRefeicaoRepository>();
        services.AddScoped<IEstudanteRepository, EstudanteRepository>();
        services.AddScoped<IRegistroConsumoRepository, RegistroConsumoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // O CumbucaDbContext também depende de IUsuarioAtual (auditoria), registrado pela API.
        return services;
    }
}
