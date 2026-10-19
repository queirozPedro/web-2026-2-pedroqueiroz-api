using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Behaviors;

/// <summary>
/// Roda os validators do request antes do handler. Se algo falhar, devolve um
/// Result com os erros em vez de lançar exceção, e o handler não é chamado.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var contexto = new ValidationContext<TRequest>(request);
        var resultados = await Task.WhenAll(validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));
        var falhas = resultados.SelectMany(r => r.Errors).ToList();

        if (falhas.Count == 0)
        {
            return await next();
        }

        var resposta = new TResponse();
        resposta.Reasons.AddRange(falhas.Select(f => new ValidacaoError(f.PropertyName, f.ErrorMessage)));
        return resposta;
    }
}

public sealed class ValidacaoError(string propriedade, string mensagem) : Error(mensagem)
{
    public string Propriedade { get; } = propriedade;
}
