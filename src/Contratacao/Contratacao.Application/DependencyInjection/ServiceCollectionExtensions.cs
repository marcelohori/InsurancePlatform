using Contratacao.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Contratacao.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContratacaoApplication(this IServiceCollection services) => services
        .AddScoped<CriarContratacaoUseCase>()
        .AddScoped<ListarContratacoesUseCase>()
        .AddScoped<ObterContratacaoPorIdUseCase>()
        .AddScoped<AtualizarContratacaoUseCase>()
        .AddScoped<DeletarContratacaoUseCase>();
}
