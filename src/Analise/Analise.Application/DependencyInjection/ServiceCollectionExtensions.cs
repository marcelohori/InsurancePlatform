using Analise.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Analise.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnaliseApplication(this IServiceCollection services) => services
        .AddScoped<ProcessarAnaliseUseCase>()
        .AddScoped<ConsultarAnalisePorPropostaUseCase>();
}
