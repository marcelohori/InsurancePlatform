using Microsoft.Extensions.DependencyInjection;
using Proposta.Application.UseCases;

namespace Proposta.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropostaApplication(this IServiceCollection services) => services
        .AddScoped<CriarPropostaUseCase>()
        .AddScoped<ListarPropostasUseCase>()
        .AddScoped<ObterPropostaPorIdUseCase>()
        .AddScoped<AtualizarPropostaUseCase>()
        .AddScoped<DeletarPropostaUseCase>();
}
